# nullinside-api

## Known Issues

1. Cannot use the `.ValueGeneratedOnAdd()`, `.ValueGeneratedOnAddOrUpdate()`, or `.ValueGeneratedOnUpdate()` in the modeling POCOs.
   * For whatever reason, these don't generate the correct SQL when you later perform an `UPDATE` on an unrelated field in the row on the POCO and call `.SaveChangesAsync()`. 