# Observed Jenkins failure responses

These eight fixtures were derived from read-only inspection of an authorized
Jenkins server on 2026-10-20. They retain the response shapes, statuses, empty
actions and test counts needed to reproduce the observed behavior.

Build numbers, stage/node identifiers, stage names, test classes and test names
were replaced. Error text was reduced to generic messages or script exit codes.
URLs, timestamps, people, parameters, environment variables, credentials, source
paths, console output and stack traces are excluded.

Cases cover failed and unstable test runs, more failures than the five-name
display limit, retained counts with missing test reports, direct stage errors,
errors found only in stage flow nodes, unstable stages without test actions, and
builds that failed before any stage was reported. The last case deliberately has
no inferred cause.
