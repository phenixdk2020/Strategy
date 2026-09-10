# PROJECT 1864 — Clean rebuild test channel

The clean tactical rebuild must not force-rewrite the historical `channel-test` branch, because that branch contains the preserved 09k–09l5 QA/documentation history.

Use the dedicated rebuild delivery branch:

`channel-rebuild`

The work branch remains:

`work/tactical-rebuild-v00.01.00`

When a rebuild gate is ready for user QA, `channel-rebuild` should point to the accepted work-branch commit. Promotion to the normal tactical channel happens only after the rebuild has passed the required gates.