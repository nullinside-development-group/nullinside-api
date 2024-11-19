# nullinside-api 

[![CodeQL](https://github.com/nullinside-development-group/nullinside-api/workflows/CodeQL/badge.svg)](https://github.com/nullinside-development-group/nullinside-api/actions?query=workflow%3ACodeQL)

## Solutions

1. Nullinside.Api: The API endpoints and controllers.
2. Nullinside.Api.Common: Shared classes.
3. Nullinside.Api.Common.AspNetCore: Shared classes that require the use of AspNetCore DLLs.
4. Nullinside.Api.Model: The code first database.
5. Nullinside.Api.Tests: The unit tests for all projects.

### Why `Nullinside.Api.Common` and `Nullinside.Api.Common.AspNetCore`

You get weird class conflicts when you import both the swagger version of AspNetCore and the latest version of
EntityFrameworkCore. I could have worked through these by pulling in more granular packages but at the end of the day
it's actually more "correct" to explicitly segregate on the line of "common functions added for this other library." So
that's the evil I chose!

## Role-Based Authentication

There are currently two roles defined in `Nullinside.Api.Model.Ddl.UserRoles`:

1. `User`: The role given to everyone.
2. `Admin`: The administrator role for the development team.
3. `VmAdmin`: Allows access to virtual machine management.

By default, all users are given the `User` role and all endpoints are configured to restrict access to someone with
the `User` role.

To provide public, unauthenticated, access to endpoints add the `[AllowAnonymous]` attribute.

To restrict endpoint access to a non-`User` role decorate it with an attribute like the
following: `[Authorize(nameof(AuthRoles.Admin))]`

### Creating Additional Roles

1. Update the `Nullinside.Api.Model.Ddl.UserRoles` class with the new role name.

Names are imported dynamically. Every name in the `UserRoles` enum will be converted to a role at runtime.

## Known Issues

1. **Error Message:** "You have an error in your SQL syntax; check the manual that corresponds to your MySQL server
   version for the right syntax to use near 'RETURNING..." on `.SaveChangesAsync()`
    1. **Solution:** Cannot use the `.ValueGeneratedOnAdd()`, `.ValueGeneratedOnAddOrUpdate()`,
       or `.ValueGeneratedOnUpdate()` in the modeling POCOs.
    2. **Description:** For whatever reason, these don't generate the correct SQL when you later perform an `UPDATE` on
       an unrelated field in the POCO and call `.SaveChangesAsync()`.
2. **Error Message:** You'll get an error during database migration that says something about a bad SQL parameter.
    1. **Solution:** Add `;AllowUserVariables=true` to your connection string.
    2. **Description:** These variables are turned off by default but EF uses them.
