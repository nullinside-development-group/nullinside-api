# nullinside-api

## Role-Based Authentication

There are currently two roles defined in `Nullinside.Api.Model.Ddl.UserRoles`:

1. `User`: The role given to everyone.
2. `Admin`: The administrator role for the development team.

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