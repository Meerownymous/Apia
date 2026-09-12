# Vendored skills

These skills are a copy of the `mattpocock-skills` plugin, checked in so every
clone of this repo has them without a local plugin install.

| | |
|---|---|
| Source | https://github.com/mattpocock/skills |
| Plugin | `mattpocock-skills@mattpocock` |
| Version | 1.2.3 |
| Commit | `3cca18b368ae95cdbdebbff572ccafa662551015` |
| License | MIT (see upstream `LICENSE`) |

The upstream repo groups skills under `skills/engineering/`, `skills/productivity/`
and so on. Project skills must sit directly under `.claude/skills/<name>/`, so the
grouping is flattened here. The 25 directories are exactly the entries the plugin
manifest declares; upstream's `in-progress`, `misc` and `deprecated` groups are
not included.

## Updating

Re-copy the manifest's skills from a newer checkout of the upstream repo and
update the version and commit above. Do not hand-edit the skill bodies — local
changes would be lost on the next update.
