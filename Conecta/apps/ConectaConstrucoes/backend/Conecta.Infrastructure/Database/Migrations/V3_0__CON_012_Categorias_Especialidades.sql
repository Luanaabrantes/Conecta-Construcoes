-- Incremental sobre V1/V2, sem recriar tabelas/usuários e sem modificar os checksums da equipe.
-- O vínculo permanece em prestadores(usuario_id), conforme a FK estabelecida em V1.
UPDATE categorias SET ativo = TRUE WHERE ativo IS NULL;
ALTER TABLE categorias ALTER COLUMN ativo SET NOT NULL;
ALTER TABLE categorias ALTER COLUMN criado_em TYPE timestamptz USING criado_em AT TIME ZONE 'UTC';
ALTER TABLE categorias ALTER COLUMN atualizado_em TYPE timestamptz USING atualizado_em AT TIME ZONE 'UTC';
UPDATE categorias SET criado_em = CURRENT_TIMESTAMP WHERE criado_em IS NULL;
UPDATE categorias SET atualizado_em = criado_em WHERE atualizado_em IS NULL;
ALTER TABLE categorias ALTER COLUMN criado_em SET NOT NULL;
ALTER TABLE categorias ALTER COLUMN atualizado_em SET NOT NULL;
ALTER TABLE categorias ALTER COLUMN descricao TYPE varchar(5000);
ALTER TABLE categorias ADD CONSTRAINT ck_categorias_slug CHECK (slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$');
-- Reutiliza a unicidade já existente; não duplica o índice.
ALTER TABLE categorias RENAME CONSTRAINT categorias_slug_key TO uq_categorias_slug;
ALTER TABLE prestador_categorias ADD COLUMN adicionado_em timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE prestador_categorias DROP CONSTRAINT fk_pc_prestador;
ALTER TABLE prestador_categorias ADD CONSTRAINT fk_pc_prestador
    FOREIGN KEY (prestador_id) REFERENCES prestadores(usuario_id) ON DELETE RESTRICT;
ALTER TABLE prestador_categorias DROP CONSTRAINT fk_pc_categoria;
ALTER TABLE prestador_categorias ADD CONSTRAINT fk_pc_categoria
    FOREIGN KEY (categoria_id) REFERENCES categorias(id) ON DELETE RESTRICT;
CREATE INDEX IF NOT EXISTS idx_pc_categoria ON prestador_categorias(categoria_id);
