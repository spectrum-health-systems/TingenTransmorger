# Tingen Transmorger: Known Issues

## Some providers have `null` or `{}` ID values

This is caused when a meeting - any meeting - has multiple entries in `Provider/Staff Names`, even if the provider has
meetings where they are the only entry in that field.

The good news is that all of a provider's meetings are displayed, regardless of how many entries are
in `Provider/Staff Names`.

### Workaround

The workaround is to search for providers by ID, and just ignore the `null` or `{}` ID value, if there is one. The data is still accurate.

### Fix

This is going to take some looking into, because looking at the original data in the Excel files, practitioners who have this issue are also doing some funky things with meetings, such as:

- Non-standard meeting titles (e.g., "Dennis" instead of "TELEHEALTH")
- Workflow is listed as "INSTANT" instead of "EHR"
- No service codes associated with the meeting