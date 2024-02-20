# nullinside-api

## Known Issues

1. **Error Message:** "You have an error in your SQL syntax; check the manual that corresponds to your MySQL server version for the right syntax to use near 'RETURNING..." on `.SaveChangesAsync()`
   1. **Solution:** Cannot use the `.ValueGeneratedOnAdd()`, `.ValueGeneratedOnAddOrUpdate()`, or `.ValueGeneratedOnUpdate()` in the modeling POCOs.
   2. **Description:** For whatever reason, these don't generate the correct SQL when you later perform an `UPDATE` on an unrelated field in the POCO and call `.SaveChangesAsync()`.