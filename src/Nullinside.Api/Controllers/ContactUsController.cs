using System.Net;
using System.Net.Mail;
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
[ApiController]
[Route("[controller]")]
public class ContactUsController : ControllerBase {
  /// <summary>
  ///   The logger.
  /// </summary>
  private static readonly ILog LOG = LogManager.GetLogger(typeof(ContactUsController));

  /// <summary>
  ///   The email host to send emails to.
  /// </summary>
  private static readonly string? EMAIL_HOST = Environment.GetEnvironmentVariable("EMAIL_HOST");

  /// <summary>
  ///   The port to connect to on the host.
  /// </summary>
  private static readonly string? EMAIL_PORT = Environment.GetEnvironmentVariable("EMAIL_PORT");

  /// <summary>
  ///   The username of the account.
  /// </summary>
  private static readonly string? EMAIL_USERNAME = Environment.GetEnvironmentVariable("EMAIL_USERNAME");

  /// <summary>
  ///   The password of the account.
  /// </summary>
  private static readonly string? EMAIL_PASSWORD = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");

  /// <summary>
  ///   The domain of the UI.
  /// </summary>
  private static readonly string? UI_DOMAIN = Environment.GetEnvironmentVariable("UI_DOMAIN");

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
  ///   Retrieves all feedback for the authenticated user.
  /// </summary>
  [HttpGet]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> GetAllFeedback(CancellationToken token = new()) {
    string? authenticatedUserId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;
    if (null == authenticatedUserId || !int.TryParse(authenticatedUserId, out int userId)) {
      return Unauthorized(false);
    }

    bool isAdmin = null != HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role &&
                                                                       Equals(c.Value, nameof(UserRoles.ADMIN)));

    // The way we specify users here isn't technically correct. We don't have usernames and we don't want to leak the
    // user or site admin's email address, so we will simplify it to say its either a comment you made or a comment
    // that the site admin made. 
    //
    // The one and only reason this works is because the site admin is the only user responding to people...when that
    // is no longer the case, this code will need to be modified.
    List<ContactUsFeedbackResponse> feedback = await _dbContext.Feedback
      .Include(f => f.Comments)
      .ThenInclude(c => c.User)
      .Include(f => f.Comments)
      .ThenInclude(c => c.FeedbackCommentReadReceipts)
      .Include(f => f.User)
      .Include(f => f.FeedbackReadReceipts)
      .Where(f => f.UserId == userId)
      .Select(f => new ContactUsFeedbackResponse(f, isAdmin, userId))
      .ToListAsync(token)
      .ConfigureAwait(false);

    return Ok(feedback);
  }

  /// <summary>
  ///   Retrieves all feedback for all users.
  /// </summary>
  [Authorize(nameof(UserRoles.ADMIN))]
  [HttpGet("admin")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> GetAllFeedbackAdmin(CancellationToken token = new()) {
    string? authenticatedUserId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;
    if (null == authenticatedUserId || !int.TryParse(authenticatedUserId, out int userId)) {
      return Unauthorized(false);
    }

    bool isAdmin = null != HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role &&
                                                                       Equals(c.Value, nameof(UserRoles.ADMIN)));

    // The way we specify users here isn't technically correct. We don't have usernames and we don't want to leak the
    // user or site admin's email address, so we will simplify it to say its either a comment you made or a comment
    // that the site admin made. 
    //
    // The one and only reason this works is because the site admin is the only user responding to people...when that
    // is no longer the case, this code will need to be modified.
    List<ContactUsFeedbackResponse> feedback = await _dbContext.Feedback
      .Include(f => f.Comments)
      .ThenInclude(c => c.User)
      .Include(f => f.Comments)
      .ThenInclude(c => c.FeedbackCommentReadReceipts)
      .Include(f => f.User)
      .Include(f => f.FeedbackReadReceipts)
      .Select(f => new ContactUsFeedbackResponse(f, isAdmin, userId))
      .ToListAsync(token)
      .ConfigureAwait(false);

    return Ok(feedback);
  }

  /// <summary>
  ///   Get a specific feedback item.
  /// </summary>
  [HttpGet("{id:int}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> GetFeedback(int id, CancellationToken token = new()) {
    string? authenticatedUserId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;
    if (null == authenticatedUserId || !int.TryParse(authenticatedUserId, out int userId)) {
      return Unauthorized(false);
    }

    bool isAdmin = null != HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role &&
                                                                       Equals(c.Value, nameof(UserRoles.ADMIN)));

    // The way we specify users here isn't technically correct. We don't have usernames and we don't want to leak the
    // user or site admin's email address, so we will simplify it to say its either a comment you made or a comment
    // that the site admin made. 
    //
    // The one and only reason this works is because the site admin is the only user responding to people...when that
    // is no longer the case, this code will need to be modified.
    ContactUsFeedbackResponse? feedback = await _dbContext.Feedback
      .Include(f => f.Comments)
      .ThenInclude(c => c.User)
      .Include(f => f.Comments)
      .ThenInclude(c => c.FeedbackCommentReadReceipts)
      .Include(f => f.User)
      .Include(f => f.FeedbackReadReceipts)
      .Where(f => (isAdmin || f.UserId == userId) && f.Id == id)
      .Select(f => new ContactUsFeedbackResponse(f, isAdmin, userId))
      .FirstOrDefaultAsync(token)
      .ConfigureAwait(false);

    return Ok(feedback);
  }

  /// <summary>
  ///   Updates the status of feedback.
  /// </summary>
  [Authorize(nameof(UserRoles.ADMIN))]
  [HttpPost("{id:int}/status")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> UpdateFeedbackStatus(int id, ContactUsFeedbackStatusChangeRequest status, CancellationToken token = new()) {
    if (null == status.Status) {
      return BadRequest("Status cannot be empty");
    }

    string? authenticatedUserId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;
    if (null == authenticatedUserId || !int.TryParse(authenticatedUserId, out int userId)) {
      return Unauthorized(false);
    }

    bool isAdmin = null != HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role &&
                                                                       Equals(c.Value, nameof(UserRoles.ADMIN)));
    if (!isAdmin) {
      return Unauthorized(false);
    }

    Feedback? existing = await _dbContext.Feedback.FirstOrDefaultAsync(f => f.Id == id, token).ConfigureAwait(false);
    if (null == existing) {
      return BadRequest(false);
    }

    existing.Status = Enum.Parse<FeedbackStatus>(status.Status);
    await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
    return Ok(true);
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

    User? admin = _dbContext.Users.FirstOrDefault(u => u.Id == Constants.ADMIN_USER_ID);
    if (null == admin || string.IsNullOrWhiteSpace(admin.Email)) {
      return Ok(false);
    }

    SendAdminEmail(admin.Email, "New Feedback", feedback.Product, feedback.Message, dbFeedback.Id);
    return Ok(true);
  }

  private void SendAdminEmail(string recipient, string subject, string product, string content, int feedbackId) {
    LOG.Info($"EMAIL_HOST: {EMAIL_HOST}");
    LOG.Info($"EMAIL_USERNAME: {EMAIL_USERNAME}");
    LOG.Info($"EMAIL_PASSWORD: {EMAIL_PASSWORD}");
    LOG.Info($"EMAIL_PORT: {EMAIL_PORT}");

    if (null == EMAIL_HOST || null == EMAIL_USERNAME || null == EMAIL_PASSWORD || null == EMAIL_PORT || !int.TryParse(EMAIL_PORT, out int port)) {
      return;
    }

    string link = $"https://{UI_DOMAIN}/contact-us/feedback/{feedbackId}";
#if DEBUG
    link = link.Replace("https://", "http://");
#endif

    var to = new MailAddress(recipient);
    var from = new MailAddress(EMAIL_USERNAME);

    var email = new MailMessage(from, to);
    email.Subject = subject;
    email.Body = $"Product: {product}\n\n{content}\n\nLink:{link}";

    var smtp = new SmtpClient();
    smtp.Host = EMAIL_HOST;
    smtp.Port = port;
    smtp.Credentials = new NetworkCredential(EMAIL_USERNAME, EMAIL_PASSWORD);
    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
    smtp.EnableSsl = true;

    try {
      /* Send method called below is what will send off our email
       * unless an exception is thrown.
       */
      smtp.Send(email);
    }
    catch (SmtpException ex) {
      LOG.Error("Failed to send notification email", ex);
    }
  }

  /// <summary>
  ///   Submits a comment attached to some feedback.
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

    bool isAdmin = null != HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role &&
                                                                       Equals(c.Value, nameof(UserRoles.ADMIN)));
    if (!isAdmin && feedback.UserId != userId) {
      return Unauthorized(false);
    }

    var dbComment = new FeedbackComment {
      FeedbackId = feedback.Id,
      UserId = userId,
      Message = comment.Comment.Trim(),
      Timestamp = DateTime.UtcNow
    };

    await _dbContext.FeedbackComment.AddAsync(dbComment, token).ConfigureAwait(false);
    await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

    User? otherPerson = null;
    if (feedback.UserId != userId) {
      otherPerson = _dbContext.Users.FirstOrDefault(u => u.Id == Constants.ADMIN_USER_ID);
    }
    else {
      otherPerson = _dbContext.Users.FirstOrDefault(u => u.Id == feedback.UserId);
    }

    if (null == otherPerson || string.IsNullOrWhiteSpace(otherPerson.Email)) {
      return Ok(false);
    }

    SendAdminEmail(otherPerson.Email, "New Feedback Comment", feedback.Product, dbComment.Message, feedback.Id);
    return Ok(true);
  }

  /// <summary>
  ///   Adds a read receipt for feedback.
  /// </summary>
  [HttpPost("{id:int}/read")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> AddFeedbackReadReceipt(int id, CancellationToken token = new()) {
    string? authenticatedUserId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;
    if (null == authenticatedUserId || !int.TryParse(authenticatedUserId, out int userId)) {
      return Unauthorized(false);
    }

    Feedback? existing = await _dbContext.Feedback.FirstOrDefaultAsync(f => f.Id == id, token).ConfigureAwait(false);
    if (null == existing) {
      return BadRequest(false);
    }

    FeedbackReadReceipt? alreadyRead = _dbContext.FeedbackReadReceipt.FirstOrDefault(r => r.FeedbackId == existing.Id && r.UserId == userId);
    if (null != alreadyRead) {
      return Ok(true);
    }

    _dbContext.FeedbackReadReceipt.Add(new FeedbackReadReceipt {
      FeedbackId = existing.Id,
      UserId = userId,
      Timestamp = DateTime.UtcNow
    });

    await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
    return Ok(true);
  }

  /// <summary>
  ///   Adds a read receipt for feedback.
  /// </summary>
  [HttpPost("{id:int}/comment/read")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ObjectResult> AddFeedbackCommentReadReceipt(int id, CancellationToken token = new()) {
    string? authenticatedUserId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;
    if (null == authenticatedUserId || !int.TryParse(authenticatedUserId, out int userId)) {
      return Unauthorized(false);
    }

    FeedbackComment? existing = await _dbContext.FeedbackComment.FirstOrDefaultAsync(f => f.Id == id, token).ConfigureAwait(false);
    if (null == existing) {
      return BadRequest(false);
    }

    FeedbackCommentReadReceipt? alreadyRead = _dbContext.FeedbackCommentReadReceipt.FirstOrDefault(r => r.FeedbackCommentId == id && r.UserId == userId);
    if (null != alreadyRead) {
      return Ok(true);
    }

    _dbContext.FeedbackCommentReadReceipt.Add(new FeedbackCommentReadReceipt {
      FeedbackCommentId = existing.Id,
      UserId = userId,
      Timestamp = DateTime.UtcNow
    });

    await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
    return Ok(true);
  }
}