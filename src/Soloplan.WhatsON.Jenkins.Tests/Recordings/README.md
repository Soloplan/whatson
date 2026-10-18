# Recorded Jenkins responses

These fixtures were captured with read-only API requests to an authorized Jenkins
server on 2026-10-18. They cover successful builds with and without changes,
failure, unstable and running builds, and a project without builds.

All identifying values have been replaced: hosts, URLs, project and folder names,
display names, descriptions, users, build numbers, timestamps, and timing values.
The public Jenkins type names, response structure, missing and empty fields, build
states, and contributor relationships are retained. Discovery is reduced to the
six recorded projects and their parent folders. No credentials, cookies, response
headers, or original server details are included.

- `discovery.json`: the three-level project discovery response.
- `job-1.json` through `job-6.json`: JSON status and the six most recent builds.
- `poll-1.xml` through `poll-6.xml`: XML polling responses for the same projects.

`RecordedResponseTests` serves these files with WireMock.Net.Minimal on a dynamic
local port. Fixture URLs are rewritten to that local server. Tests never proxy,
record, or connect to the original server, and they do not trigger builds.
