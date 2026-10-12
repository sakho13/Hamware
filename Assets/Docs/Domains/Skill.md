# Skill

Skill は社員が持つ基本的なスキルを表します。

- スキルは種類とレベルを持つ。
- スキルの種類は Frontend, Backend, Database, Infrastructure, Testing, Security, Architecture, Planning, Management, Communication, Sales, CustomerSupport の12種類。
- レベルは1〜5の範囲で表現される。
- 一人の社員は異なる種類のスキルを複数保有できる。
- 同じ種類のスキルを再登録すると、レベルが上書きされる。
- 将来的にゲーム内でスキルレベルが成長する仕組みを想定しているが、成長のロジック自体はまだ実装していない。
- React や PostgreSQL のような個別の技術スキルは対象外で、将来的に別のドメインとして扱う想定。
