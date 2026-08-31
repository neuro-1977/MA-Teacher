# Trusted learning sources

MA-Teacher uses a simple rule: teaching evidence comes from an official education, government, public-service, awarding-body or established subject institution source. A search result, social post, news article or confident AI answer is not curriculum evidence.

## The source ladder

1. Current statutory curriculum and official government guidance.
2. Official awarding-body specifications where the learner's course uses that body.
3. Official public institutions and established subject organisations.
4. Teacher-reviewed local material whose origin and date are recorded.

Lower items may explain higher items. They must not silently replace or contradict them.

## Network enforcement

Curriculum refreshes are checked before MA-Teacher sends a request and after every redirect:

- HTTPS is required.
- IP-address URLs, embedded credentials and unusual ports are rejected.
- Hostnames must match an explicit allowlist; lookalike suffixes are rejected.
- BBC material is limited to the Bitesize path. A redirect to BBC News is rejected.
- Redirect chains are bounded and every destination is checked again.
- General news, social media, search engines, content farms and arbitrary file hosts are not allowed as evidence sources.

This is defence in depth, not a claim that every page on an approved organisation's site is suitable for every lesson. Imported evidence still needs source identity, date, scope and teacher review.

## Adding a source

A new host is a code-reviewed policy change. Record who governs the source, why it is suitable, which age/stage/subject it serves, how updates are dated, and whether a narrower path rule is safer than trusting the whole host.
