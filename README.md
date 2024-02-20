# nullinside-api

## Role-Based Authentication

There are currently two roles defined in `Nullinside.Api.Common.AuthRoles`:
1. `User`: The role given to everyone.
2. `Admin`: The administrator role for the development team.

By default, all users are given the `User` role and all endpoints are configured to restrict access to someone with the `User` role.

To provide public, unauthenticated, access to endpoints add the `[AllowAnonymous]` attribute.

To restrict endpoint access to a non-`User` role decorate it with an attribute like the following: `[Authorize(AuthRoles.ADMIN)]`

## Known Issues

1. **Error Message:** "You have an error in your SQL syntax; check the manual that corresponds to your MySQL server version for the right syntax to use near 'RETURNING..." on `.SaveChangesAsync()`
   1. **Solution:** Cannot use the `.ValueGeneratedOnAdd()`, `.ValueGeneratedOnAddOrUpdate()`, or `.ValueGeneratedOnUpdate()` in the modeling POCOs.
   2. **Description:** For whatever reason, these don't generate the correct SQL when you later perform an `UPDATE` on an unrelated field in the POCO and call `.SaveChangesAsync()`.