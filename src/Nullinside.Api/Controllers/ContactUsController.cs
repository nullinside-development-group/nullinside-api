using System.Security.Claims;

using log4net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nullinside.Api.Common;
using Nullinside.Api.Model;
using Nullinside.Api.Model.Ddl;
using Nullinside.Api.Shared.Json;

namespace Nullinside.Api.Controllers;

/// <summary>
///   Provides the ability to read and write feedback for the website.
/// </summary>
[Authorize(nameof(UserRoles.VM_ADMIN))]
[ApiController]
[Route("[controller]")]
public class ContactUsController : ControllerBase {
  /// <summary>
  ///   The nullinside database.
  /// </summary>
  private readonly INullinsideContext _dbContext;

  /// <summary>
  ///   The logger.
  /// </summary>
  private readonly ILog _logger = LogManager.GetLogger(typeof(DockerController));

  /// <summary>
  ///   Initializes a new instance of the <see cref="ContactUsController" /> class.
  /// </summary>
  /// <param name="dbContext">The nullinside database.</param>
  public ContactUsController(INullinsideContext dbContext) {
    _dbContext = dbContext;
  }

  /// <summary>
  ///   Submits new feedback to the website.
  /// </summary>
  [HttpGet]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> GetAllFeedback(CancellationToken token = new()) {
    string? authenticatedUserId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;
    if (null == authenticatedUserId || !int.TryParse(authenticatedUserId, out int userId)) {
      return Unauthorized(false);
    }

    // The way we specify users here isn't technically correct. We don't have usernames and we don't want to leak the
    // user or site admin's email address, so we will simplify it to say its either a comment you made or a comment
    // that the site admin made. 
    //
    // The one and only reason this works is because the site admin is the only user responding to people...when that
    // is no longer the case, this code will need to be modified.
    var feedback = await _dbContext.Feedback
      .Include(f => f.Comments)
      .Where(f => f.UserId == userId)
      .Select(f => new ContactUsFeedbackResponse(f))
      .ToListAsync(token)
      .ConfigureAwait(false);

    return Ok(feedback);
  }
  
  /// <summary>
  ///   Submits new feedback to the website.
  /// </summary>
  [HttpGet("{id:int}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> GetFeedback(int id, CancellationToken token = new()) {
    string? authenticatedUserId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;
    if (null == authenticatedUserId || !int.TryParse(authenticatedUserId, out int userId)) {
      return Unauthorized(false);
    }

    // The way we specify users here isn't technically correct. We don't have usernames and we don't want to leak the
    // user or site admin's email address, so we will simplify it to say its either a comment you made or a comment
    // that the site admin made. 
    //
    // The one and only reason this works is because the site admin is the only user responding to people...when that
    // is no longer the case, this code will need to be modified.
    var feedback = await _dbContext.Feedback
      .Include(f => f.Comments)
      .Where(f => f.UserId == userId && f.Id == id)
      .Select(f => new ContactUsFeedbackResponse(f))
      .FirstOrDefaultAsync(token)
      .ConfigureAwait(false);

    return Ok(feedback);
  }

  /// <summary>
  ///   Submits new feedback to the website.
  /// </summary>
  [HttpPost]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> AddFeedback(ContactUsFeedback feedback, CancellationToken token = new()) {
    if (string.IsNullOrWhiteSpace(feedback.Product) || string.IsNullOrWhiteSpace(feedback.Message)) {
      return BadRequest("Product and message cannot be empty");
    }
    
    string? authenticatedUserId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;
    if (null == authenticatedUserId || !int.TryParse(authenticatedUserId, out int userId)) {
      return Unauthorized(false);
    }

    var dbFeedback = new Feedback {
      Product = feedback.Product.Trim(),
      Message = feedback.Message.Trim(),
      UserId = userId,
      Status = FeedbackStatus.Open,
      Timestamp = DateTime.UtcNow
    };

    await _dbContext.Feedback.AddAsync(dbFeedback, token).ConfigureAwait(false);
    await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
    return Ok(true);
  }

  /// <summary>
  ///   Submits a comment against the website.
  /// </summary>
  [HttpPost("{id:int}/comment")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> AddFeedbackComment(int id, ContactUsFeedbackComment comment, CancellationToken token = new()) {
    if (string.IsNullOrWhiteSpace(comment.Comment)) {
      return BadRequest("Comment cannot be empty");
    }

    string? authenticatedUserId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;
    if (null == authenticatedUserId || !int.TryParse(authenticatedUserId, out int userId)) {
      return Unauthorized(false);
    }

    Feedback? feedback = await _dbContext.Feedback.FirstOrDefaultAsync(f => f.Id == id, token).ConfigureAwait(false);
    if (null == feedback) {
      return BadRequest("Feedback not found");
    }

    var dbComment = new FeedbackComment {
      FeedbackId = feedback.Id,
      UserId = userId,
      Message = comment.Comment.Trim(),
      Timestamp = DateTime.UtcNow
    };

    await _dbContext.FeedbackComment.AddAsync(dbComment, token).ConfigureAwait(false);
    await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
    return Ok(true);
  }
}