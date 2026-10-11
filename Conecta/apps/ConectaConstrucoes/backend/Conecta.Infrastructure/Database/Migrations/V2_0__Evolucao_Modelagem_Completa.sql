-- ============================================================================
-- V2_0__Evolucao_Modelagem_Completa.sql
-- Conecta - Migração Incremental para o Schema Completo
-- ============================================================================

-- 1. TIPOS PERSONALIZADOS (ENUMS)
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'status_disponibilidade') THEN
        CREATE TYPE status_disponibilidade AS ENUM ('DISPONIVEL', 'OCUPADO', 'AUSENTE', 'INATIVO');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'status_solicitacao') THEN
        CREATE TYPE status_solicitacao AS ENUM ('ABERTA', 'AGUARDANDO_PROPOSTA', 'PROPOSTA_RECEBIDA', 'ACEITA', 'RECUSADA', 'CANCELADA');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'status_proposta') THEN
        CREATE TYPE status_proposta AS ENUM ('ENVIADA', 'VISUALIZADA', 'ACEITA', 'RECUSADA', 'EXPIRADA');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'status_pedido') THEN
        CREATE TYPE status_pedido AS ENUM ('AGUARDANDO_ACEITE', 'AGENDADO', 'EM_EXECUCAO', 'CONCLUIDO', 'CANCELADO');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'tipo_mensagem') THEN
        CREATE TYPE tipo_mensagem AS ENUM ('TEXTO', 'IMAGEM', 'AUDIO', 'VIDEO', 'ARQUIVO');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'status_verificacao') THEN
        CREATE TYPE status_verificacao AS ENUM ('PENDENTE_DOCS', 'DOCUMENTOS_RECEBIDOS', 'DOCUMENTOS_VERIFICADOS', 'VERIFICADO', 'REJEITADO', 'COMPLETO');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'status_conta') THEN
        CREATE TYPE status_conta AS ENUM ('ATIVA', 'BLOQUEADA', 'SUSPENSA', 'DELETADA');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'status_documento') THEN
        CREATE TYPE status_documento AS ENUM ('PENDENTE', 'APROVADO', 'REJEITADO');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'tipo_denuncia') THEN
        CREATE TYPE tipo_denuncia AS ENUM ('COMPORTAMENTO_INAPROPRIADO', 'FRAUDE', 'CONTEUDO_ILEGAL', 'PERFIL_FALSO', 'NAO_COMPLETOU_SERVICO', 'COBRANCA_INDEVIDA', 'OUTRO');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'status_denuncia') THEN
        CREATE TYPE status_denuncia AS ENUM ('ABERTA', 'ANALISANDO', 'ENCERRADA', 'RESOLVIDA', 'BANIDO');
    END IF;
END $$;

-- 2. RBAC (PAPÉIS E PERMISSÕES)
CREATE TABLE IF NOT EXISTS roles (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(50) UNIQUE NOT NULL,
    descricao TEXT,
    ativo BOOLEAN DEFAULT TRUE,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS permissoes (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) UNIQUE NOT NULL,
    descricao TEXT,
    recurso VARCHAR(50),
    acao VARCHAR(50),
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS role_permissoes (
    role_id INT NOT NULL,
    permissao_id INT NOT NULL,
    PRIMARY KEY (role_id, permissao_id),
    CONSTRAINT fk_role_permissoes_role FOREIGN KEY(role_id) REFERENCES roles(id) ON DELETE CASCADE,
    CONSTRAINT fk_role_permissoes_permissao FOREIGN KEY(permissao_id) REFERENCES permissoes(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS usuario_roles (
    usuario_id INT NOT NULL,
    role_id INT NOT NULL,
    atribuido_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (usuario_id, role_id),
    CONSTRAINT fk_usuario_roles_usuario FOREIGN KEY(usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE,
    CONSTRAINT fk_usuario_roles_role FOREIGN KEY(role_id) REFERENCES roles(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_usuario_roles_role_id ON usuario_roles(role_id);

-- 3. EXPANSÃO DA TABELA: USUARIOS
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS nome VARCHAR(150);
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS status_verificacao status_verificacao DEFAULT 'COMPLETO';
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS status_conta status_conta DEFAULT 'ATIVA';
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS ultimo_acesso_em TIMESTAMP;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

UPDATE usuarios SET nome = SPLIT_PART(email, '@', 1) WHERE nome IS NULL;

CREATE INDEX IF NOT EXISTS idx_usuarios_email ON usuarios(email);
CREATE INDEX IF NOT EXISTS idx_usuarios_status_verificacao ON usuarios(status_verificacao);
CREATE INDEX IF NOT EXISTS idx_usuarios_status_conta ON usuarios(status_conta);

-- 4. EXPANSÃO DA TABELA: TELEFONES
ALTER TABLE telefones ADD COLUMN IF NOT EXISTS criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
CREATE INDEX IF NOT EXISTS idx_telefones_usuario_id ON telefones(usuario_id);

-- 5. EXPANSÃO DA TABELA: ENDERECOS
ALTER TABLE enderecos ADD COLUMN IF NOT EXISTS apelido VARCHAR(50) DEFAULT 'Principal';
ALTER TABLE enderecos ADD COLUMN IF NOT EXISTS logradouro VARCHAR(200) DEFAULT '';
ALTER TABLE enderecos ADD COLUMN IF NOT EXISTS numero VARCHAR(20) DEFAULT 'S/N';
ALTER TABLE enderecos ADD COLUMN IF NOT EXISTS complemento VARCHAR(100);
ALTER TABLE enderecos ADD COLUMN IF NOT EXISTS cidade VARCHAR(100) DEFAULT '';
ALTER TABLE enderecos ADD COLUMN IF NOT EXISTS estado CHAR(2) DEFAULT 'SP';
ALTER TABLE enderecos ADD COLUMN IF NOT EXISTS criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE enderecos ADD COLUMN IF NOT EXISTS atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

CREATE INDEX IF NOT EXISTS idx_enderecos_usuario_id ON enderecos(usuario_id);
CREATE INDEX IF NOT EXISTS idx_enderecos_principal ON enderecos(usuario_id, principal);

-- 6. TABELAS: CLIENTE E PRESTADOR
CREATE TABLE IF NOT EXISTS cliente (
    usuario_id INT PRIMARY KEY,
    ativo BOOLEAN DEFAULT TRUE,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_cliente_usuario FOREIGN KEY(usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS prestador (
    usuario_id INT PRIMARY KEY,
    nome_fantasia VARCHAR(150),
    bio TEXT,
    experiencia_anos INTEGER,
    valor_hora_inicial NUMERIC(10,2),
    capa_url VARCHAR(500),
    latitude NUMERIC(10,8) NOT NULL DEFAULT 0,
    longitude NUMERIC(11,8) NOT NULL DEFAULT 0,
    raio_atendimento_km INTEGER DEFAULT 15,
    status_disponibilidade status_disponibilidade DEFAULT 'DISPONIVEL',
    plano_assinatura CHAR(1) DEFAULT 'G',
    destaque_regiao BOOLEAN DEFAULT FALSE,
    selo_superprestador BOOLEAN DEFAULT FALSE,
    tempo_medio_resposta_minutos INTEGER,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_prestador_usuario FOREIGN KEY(usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_prestador_latitude_longitude ON prestador(latitude, longitude);
CREATE INDEX IF NOT EXISTS idx_prestador_status_disponibilidade ON prestador(status_disponibilidade);

-- 7. TABELAS DE DOCUMENTOS
CREATE TABLE IF NOT EXISTS documentos_prestador (
    id SERIAL PRIMARY KEY,
    prestador_id INT NOT NULL,
    tipo_documento VARCHAR(50) NOT NULL,
    arquivo_url VARCHAR(500) NOT NULL,
    status status_documento DEFAULT 'PENDENTE',
    motivo_rejeicao TEXT,
    submetido_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    analisado_em TIMESTAMP,
    analisado_por INT,
    CONSTRAINT fk_documento_prestador FOREIGN KEY(prestador_id) REFERENCES prestador(usuario_id) ON DELETE CASCADE,
    CONSTRAINT fk_documento_analisado_por FOREIGN KEY(analisado_por) REFERENCES usuarios(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_documentos_prestador_id ON documentos_prestador(prestador_id);
CREATE INDEX IF NOT EXISTS idx_documentos_prestador_status ON documentos_prestador(status);

CREATE TABLE IF NOT EXISTS documentos_cliente (
    id SERIAL PRIMARY KEY,
    cliente_id INT NOT NULL,
    tipo_documento VARCHAR(50) NOT NULL,
    arquivo_url VARCHAR(500) NOT NULL,
    status status_documento DEFAULT 'PENDENTE',
    motivo_rejeicao TEXT,
    submetido_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    analisado_em TIMESTAMP,
    analisado_por INT,
    CONSTRAINT fk_documento_cliente FOREIGN KEY(cliente_id) REFERENCES cliente(usuario_id) ON DELETE CASCADE,
    CONSTRAINT fk_documento_cliente_analisado_por FOREIGN KEY(analisado_por) REFERENCES usuarios(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_documentos_cliente_id ON documentos_cliente(cliente_id);
CREATE INDEX IF NOT EXISTS idx_documentos_cliente_status ON documentos_cliente(status);

-- 8. EXPANSÃO: CATEGORIAS
ALTER TABLE categorias ADD COLUMN IF NOT EXISTS ativo BOOLEAN DEFAULT TRUE;
ALTER TABLE categorias ADD COLUMN IF NOT EXISTS criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE categorias ADD COLUMN IF NOT EXISTS atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
CREATE INDEX IF NOT EXISTS idx_categorias_slug ON categorias(slug);
CREATE INDEX IF NOT EXISTS idx_categorias_ativo ON categorias(ativo);

-- 9. SERVIÇOS
CREATE TABLE IF NOT EXISTS servicos (
    id SERIAL PRIMARY KEY,
    prestador_id INT NOT NULL,
    categoria_id INT,
    titulo VARCHAR(200) NOT NULL,
    descricao TEXT,
    valor_cobrado NUMERIC(10,2),
    data_realizacao DATE,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_servico_prestador FOREIGN KEY(prestador_id) REFERENCES prestador(usuario_id) ON DELETE CASCADE,
    CONSTRAINT fk_servico_categoria FOREIGN KEY(categoria_id) REFERENCES categorias(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_servicos_prestador_id ON servicos(prestador_id);
CREATE INDEX IF NOT EXISTS idx_servicos_categoria_id ON servicos(categoria_id);
CREATE INDEX IF NOT EXISTS idx_servicos_data_realizacao ON servicos(data_realizacao);

CREATE TABLE IF NOT EXISTS servico_imagens (
    id SERIAL PRIMARY KEY,
    servico_id INT NOT NULL,
    imagem_url VARCHAR(500) NOT NULL,
    ordem INTEGER DEFAULT 1,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_servico_imagem FOREIGN KEY(servico_id) REFERENCES servicos(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_servico_imagens_servico_id ON servico_imagens(servico_id);

-- 10. PORTFOLIOS (FEED E ESTRUTURA EXPANDIDA)
CREATE TABLE IF NOT EXISTS portfolios (
    id SERIAL PRIMARY KEY,
    prestador_id INT NOT NULL,
    titulo VARCHAR(200),
    descricao TEXT,
    destaque BOOLEAN DEFAULT FALSE,
    total_visualizacoes INTEGER DEFAULT 0,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_portfolio_prestador FOREIGN KEY(prestador_id) REFERENCES prestador(usuario_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_portfolio_prestador_id ON portfolios(prestador_id);
CREATE INDEX IF NOT EXISTS idx_portfolio_destaque ON portfolios(destaque);

CREATE TABLE IF NOT EXISTS portfolio_imagens (
    id SERIAL PRIMARY KEY,
    portfolio_id INT NOT NULL,
    imagem_url VARCHAR(500) NOT NULL,
    ordem INTEGER DEFAULT 1,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_portfolio_imagem FOREIGN KEY(portfolio_id) REFERENCES portfolios(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_portfolio_imagens_portfolio_id ON portfolio_imagens(portfolio_id);

-- 11. FAVORITOS
CREATE TABLE IF NOT EXISTS favoritos (
    cliente_id INT NOT NULL,
    prestador_id INT NOT NULL,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (cliente_id, prestador_id),
    CONSTRAINT fk_favorito_cliente FOREIGN KEY(cliente_id) REFERENCES cliente(usuario_id) ON DELETE CASCADE,
    CONSTRAINT fk_favorito_prestador FOREIGN KEY(prestador_id) REFERENCES prestador(usuario_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_favoritos_cliente_id ON favoritos(cliente_id);

-- 12. SOLICITAÇÕES
CREATE TABLE IF NOT EXISTS solicitacoes (
    id SERIAL PRIMARY KEY,
    cliente_id INT NOT NULL,
    prestador_id INT NOT NULL,
    categoria_id INT,
    endereco_id INT,
    endereco_atendimento TEXT,
    titulo VARCHAR(255) NOT NULL,
    descricao TEXT NOT NULL,
    status status_solicitacao DEFAULT 'ABERTA',
    latitude NUMERIC(10,8),
    longitude NUMERIC(11,8),
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_solicitacao_cliente FOREIGN KEY(cliente_id) REFERENCES cliente(usuario_id) ON DELETE CASCADE,
    CONSTRAINT fk_solicitacao_prestador FOREIGN KEY(prestador_id) REFERENCES prestador(usuario_id) ON DELETE CASCADE,
    CONSTRAINT fk_solicitacao_categoria FOREIGN KEY(categoria_id) REFERENCES categorias(id) ON DELETE SET NULL,
    CONSTRAINT fk_solicitacao_endereco FOREIGN KEY(endereco_id) REFERENCES enderecos(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_solicitacoes_cliente_id ON solicitacoes(cliente_id);
CREATE INDEX IF NOT EXISTS idx_solicitacoes_prestador_id ON solicitacoes(prestador_id);
CREATE INDEX IF NOT EXISTS idx_solicitacoes_status ON solicitacoes(status);
CREATE INDEX IF NOT EXISTS idx_solicitacoes_criado_em ON solicitacoes(criado_em DESC);

CREATE TABLE IF NOT EXISTS solicitacao_imagens (
    id SERIAL PRIMARY KEY,
    solicitacao_id INT NOT NULL,
    imagem_url VARCHAR(500) NOT NULL,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_solicitacao_imagem FOREIGN KEY(solicitacao_id) REFERENCES solicitacoes(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_solicitacao_imagens_solicitacao_id ON solicitacao_imagens(solicitacao_id);

-- 13. PROPOSTAS
CREATE TABLE IF NOT EXISTS propostas (
    id SERIAL PRIMARY KEY,
    solicitacao_id INT NOT NULL,
    prestador_id INT NOT NULL,
    valor_total NUMERIC(10,2) NOT NULL,
    prazo_dias INTEGER,
    descricao TEXT,
    status status_proposta DEFAULT 'ENVIADA',
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_proposta_solicitacao FOREIGN KEY(solicitacao_id) REFERENCES solicitacoes(id) ON DELETE CASCADE,
    CONSTRAINT fk_proposta_prestador FOREIGN KEY(prestador_id) REFERENCES prestador(usuario_id) ON DELETE CASCADE,
    CONSTRAINT ck_valor_total CHECK (valor_total > 0),
    CONSTRAINT ck_prazo_dias CHECK (prazo_dias > 0 OR prazo_dias IS NULL)
);

CREATE INDEX IF NOT EXISTS idx_propostas_solicitacao_id ON propostas(solicitacao_id);
CREATE INDEX IF NOT EXISTS idx_propostas_prestador_id ON propostas(prestador_id);
CREATE INDEX IF NOT EXISTS idx_propostas_status ON propostas(status);
CREATE INDEX IF NOT EXISTS idx_propostas_criado_em ON propostas(criado_em DESC);

CREATE TABLE IF NOT EXISTS proposta_itens (
    id SERIAL PRIMARY KEY,
    proposta_id INT NOT NULL,
    descricao VARCHAR(255),
    valor NUMERIC(10,2),
    quantidade INT DEFAULT 1,
    CONSTRAINT fk_proposta_item FOREIGN KEY(proposta_id) REFERENCES propostas(id) ON DELETE CASCADE,
    CONSTRAINT ck_valor_item CHECK (valor > 0),
    CONSTRAINT ck_quantidade_item CHECK (quantidade > 0)
);

CREATE INDEX IF NOT EXISTS idx_proposta_itens_proposta_id ON proposta_itens(proposta_id);

-- 14. EXPANSÃO: PEDIDOS
ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS proposta_id INT;
ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS endereco_id INT;
ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS endereco_atendimento TEXT;
ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS valor_final NUMERIC(10,2);
ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS agendado_para TIMESTAMP;
ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS iniciado_em TIMESTAMP;
ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS concluido_em TIMESTAMP;
ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

CREATE INDEX IF NOT EXISTS idx_pedidos_cliente_id ON pedidos(cliente_id);
CREATE INDEX IF NOT EXISTS idx_pedidos_prestador_id ON pedidos(prestador_id);

-- 15. CONVERSAS E MENSAGENS
CREATE TABLE IF NOT EXISTS conversas (
    id SERIAL PRIMARY KEY,
    pedido_id INT,
    cliente_id INT NOT NULL,
    prestador_id INT NOT NULL,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_conversa_pedido FOREIGN KEY(pedido_id) REFERENCES pedidos(id) ON DELETE SET NULL,
    CONSTRAINT fk_conversa_cliente FOREIGN KEY(cliente_id) REFERENCES cliente(usuario_id) ON DELETE CASCADE,
    CONSTRAINT fk_conversa_prestador FOREIGN KEY(prestador_id) REFERENCES prestador(usuario_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_conversas_pedido_id ON conversas(pedido_id);
CREATE INDEX IF NOT EXISTS idx_conversas_cliente_id ON conversas(cliente_id);
CREATE INDEX IF NOT EXISTS idx_conversas_prestador_id ON conversas(prestador_id);

CREATE TABLE IF NOT EXISTS mensagens (
    id SERIAL PRIMARY KEY,
    conversa_id INT NOT NULL,
    remetente_id INT NOT NULL,
    tipo tipo_mensagem DEFAULT 'TEXTO',
    conteudo TEXT,
    lida BOOLEAN DEFAULT FALSE,
    lida_em TIMESTAMP,
    enviado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_mensagem_conversa FOREIGN KEY(conversa_id) REFERENCES conversas(id) ON DELETE CASCADE,
    CONSTRAINT fk_mensagem_remetente FOREIGN KEY(remetente_id) REFERENCES usuarios(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_mensagens_conversa_id ON mensagens(conversa_id);
CREATE INDEX IF NOT EXISTS idx_mensagens_remetente_id ON mensagens(remetente_id);

CREATE TABLE IF NOT EXISTS mensagem_anexos (
    id SERIAL PRIMARY KEY,
    mensagem_id INT NOT NULL,
    arquivo_url VARCHAR(500),
    tipo_arquivo VARCHAR(30),
    tamanho_bytes INT,
    CONSTRAINT fk_anexo_mensagem FOREIGN KEY(mensagem_id) REFERENCES mensagens(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_mensagem_anexos_mensagem_id ON mensagem_anexos(mensagem_id);

-- 16. EXPANSÃO: AVALIAÇÕES
ALTER TABLE avaliacoes ADD COLUMN IF NOT EXISTS atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

-- 17. DENÚNCIAS
CREATE TABLE IF NOT EXISTS denuncias (
    id SERIAL PRIMARY KEY,
    usuario_denunciante_id INT NOT NULL,
    usuario_denunciado_id INT NOT NULL,
    tipo_denuncia tipo_denuncia NOT NULL,
    motivo TEXT NOT NULL,
    descricao TEXT,
    status status_denuncia DEFAULT 'ABERTA',
    analisado_por INT,
    resultado TEXT,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    analisado_em TIMESTAMP,
    CONSTRAINT fk_denuncia_denunciante FOREIGN KEY(usuario_denunciante_id) REFERENCES usuarios(id) ON DELETE CASCADE,
    CONSTRAINT fk_denuncia_denunciado FOREIGN KEY(usuario_denunciado_id) REFERENCES usuarios(id) ON DELETE CASCADE,
    CONSTRAINT fk_denuncia_analisado_por FOREIGN KEY(analisado_por) REFERENCES usuarios(id) ON DELETE SET NULL,
    CONSTRAINT ck_usuarios_diferentes CHECK (usuario_denunciante_id != usuario_denunciado_id)
);

-- 18. AUDITORIA E NOTIFICAÇÕES
CREATE TABLE IF NOT EXISTS audit_log (
    id SERIAL PRIMARY KEY,
    usuario_id INT,
    tabela_afetada VARCHAR(100) NOT NULL,
    id_registro INT,
    acao VARCHAR(50) NOT NULL,
    dados_antes JSONB,
    dados_depois JSONB,
    ip_address VARCHAR(45),
    user_agent TEXT,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_audit_usuario FOREIGN KEY(usuario_id) REFERENCES usuarios(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS notificacoes (
    id SERIAL PRIMARY KEY,
    usuario_id INT NOT NULL,
    titulo VARCHAR(150),
    mensagem TEXT,
    tipo VARCHAR(50),
    referencia_tabela VARCHAR(50),
    referencia_id INT,
    lida BOOLEAN DEFAULT FALSE,
    lida_em TIMESTAMP,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_notificacao_usuario FOREIGN KEY(usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_notificacoes_usuario_id ON notificacoes(usuario_id);

-- 19. CARGA INICIAL: ROLES E PERMISSÕES
INSERT INTO roles (nome, descricao)
VALUES
    ('CLIENTE', 'Acesso às funcionalidades de cliente'),
    ('PRESTADOR', 'Acesso às funcionalidades de prestador'),
    ('ADMIN_MASTER', 'Acesso total ao painel administrativo'),
    ('SUPORTE', 'Atendimento e consulta de usuários'),
    ('MODERACAO', 'Análise de denúncias e conteúdos'),
    ('FINANCEIRO', 'Consulta e gestão financeira'),
    ('ANALISTA', 'Consulta de dados e relatórios')
ON CONFLICT (nome) DO NOTHING;

INSERT INTO permissoes (nome, descricao, recurso, acao)
VALUES
    ('USUARIOS_VISUALIZAR', 'Visualizar usuários', 'USUARIOS', 'VISUALIZAR'),
    ('USUARIOS_EDITAR', 'Editar usuários', 'USUARIOS', 'EDITAR'),
    ('PRESTADORES_VISUALIZAR', 'Visualizar prestadores', 'PRESTADORES', 'VISUALIZAR'),
    ('PRESTADORES_APROVAR', 'Aprovar documentos de prestadores', 'PRESTADORES', 'APROVAR'),
    ('CATEGORIAS_VISUALIZAR', 'Visualizar categorias', 'CATEGORIAS', 'VISUALIZAR'),
    ('CATEGORIAS_EDITAR', 'Criar, editar e desativar categorias', 'CATEGORIAS', 'EDITAR'),
    ('DENUNCIAS_VISUALIZAR', 'Visualizar denúncias', 'DENUNCIAS', 'VISUALIZAR'),
    ('DENUNCIAS_ANALISAR', 'Analisar e encerrar denúncias', 'DENUNCIAS', 'ANALISAR'),
    ('MENSAGENS_MODERAR', 'Moderar mensagens denunciadas', 'MENSAGENS', 'MODERAR'),
    ('FINANCEIRO_VISUALIZAR', 'Visualizar informações financeiras', 'FINANCEIRO', 'VISUALIZAR'),
    ('FINANCEIRO_EDITAR', 'Editar informações financeiras', 'FINANCEIRO', 'EDITAR'),
    ('RELATORIOS_VISUALIZAR', 'Visualizar relatórios', 'RELATORIOS', 'VISUALIZAR'),
    ('SOLICITACOES_CRIAR', 'Criar solicitações de serviço', 'SOLICITACOES', 'CRIAR'),
    ('PROPOSTAS_CRIAR', 'Enviar propostas de serviço', 'PROPOSTAS', 'CRIAR'),
    ('PEDIDOS_VISUALIZAR', 'Visualizar pedidos relacionados ao usuário', 'PEDIDOS', 'VISUALIZAR'),
    ('AVALIACOES_CRIAR', 'Criar avaliações de pedidos concluídos', 'AVALIACOES', 'CRIAR')
ON CONFLICT (nome) DO NOTHING;

INSERT INTO role_permissoes (role_id, permissao_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissoes p
WHERE r.nome = 'ADMIN_MASTER'
ON CONFLICT DO NOTHING;

-- 20. VIEWS
CREATE OR REPLACE VIEW vw_prestadores_ativos AS
SELECT 
    p.usuario_id,
    u.nome,
    p.nome_fantasia,
    p.capa_url,
    u.email,
    p.latitude,
    p.longitude,
    p.raio_atendimento_km,
    p.status_disponibilidade,
    p.selo_superprestador,
    p.tempo_medio_resposta_minutos,
    u.media_avaliacao,
    u.total_avaliacoes,
    STRING_AGG(DISTINCT c.nome, ', ') AS categorias
FROM prestador p
INNER JOIN usuarios u ON p.usuario_id = u.id
LEFT JOIN prestador_categorias pc ON p.usuario_id = pc.prestador_id
LEFT JOIN categorias c ON pc.categoria_id = c.id
WHERE u.status_conta = 'ATIVA' 
    AND p.status_disponibilidade != 'INATIVO'
GROUP BY p.usuario_id, u.nome, p.nome_fantasia, p.capa_url, u.email, p.latitude, p.longitude,
         p.raio_atendimento_km, p.status_disponibilidade, p.selo_superprestador,
         p.tempo_medio_resposta_minutos, u.media_avaliacao, u.total_avaliacoes;

CREATE OR REPLACE VIEW vw_estatisticas_prestador AS
SELECT 
    p.usuario_id,
    u.nome,
    COUNT(DISTINCT pd.id) AS total_pedidos,
    COUNT(DISTINCT av.id) AS total_avaliacoes,
    AVG(av.nota) AS media_avaliacoes,
    COUNT(DISTINCT s.id) AS total_servicos_realizados
FROM prestador p
LEFT JOIN pedidos pd ON p.usuario_id = pd.prestador_id
LEFT JOIN avaliacoes av ON p.usuario_id = av.prestador_id
LEFT JOIN servicos s ON p.usuario_id = s.prestador_id
LEFT JOIN usuarios u ON p.usuario_id = u.id
GROUP BY p.usuario_id, u.nome;

-- 21. PROCEDURES E TRIGGERS
CREATE OR REPLACE FUNCTION update_atualizado_em()
RETURNS TRIGGER AS $$
BEGIN
    NEW.atualizado_em = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_usuarios_atualizado_em ON usuarios;
CREATE TRIGGER trigger_usuarios_atualizado_em
BEFORE UPDATE ON usuarios
FOR EACH ROW
EXECUTE FUNCTION update_atualizado_em();

DROP TRIGGER IF EXISTS trigger_prestador_atualizado_em ON prestador;
CREATE TRIGGER trigger_prestador_atualizado_em
BEFORE UPDATE ON prestador
FOR EACH ROW
EXECUTE FUNCTION update_atualizado_em();

DROP TRIGGER IF EXISTS trigger_cliente_atualizado_em ON cliente;
CREATE TRIGGER trigger_cliente_atualizado_em
BEFORE UPDATE ON cliente
FOR EACH ROW
EXECUTE FUNCTION update_atualizado_em();

DROP TRIGGER IF EXISTS trigger_enderecos_atualizado_em ON enderecos;
CREATE TRIGGER trigger_enderecos_atualizado_em
BEFORE UPDATE ON enderecos
FOR EACH ROW
EXECUTE FUNCTION update_atualizado_em();

DROP TRIGGER IF EXISTS trigger_conversas_atualizado_em ON conversas;
CREATE TRIGGER trigger_conversas_atualizado_em
BEFORE UPDATE ON conversas
FOR EACH ROW
EXECUTE FUNCTION update_atualizado_em();

DROP TRIGGER IF EXISTS trigger_avaliacoes_atualizado_em ON avaliacoes;
CREATE TRIGGER trigger_avaliacoes_atualizado_em
BEFORE UPDATE ON avaliacoes
FOR EACH ROW
EXECUTE FUNCTION update_atualizado_em();

DROP TRIGGER IF EXISTS trigger_pedidos_atualizado_em ON pedidos;
CREATE TRIGGER trigger_pedidos_atualizado_em
BEFORE UPDATE ON pedidos
FOR EACH ROW
EXECUTE FUNCTION update_atualizado_em();

DROP TRIGGER IF EXISTS trigger_solicitacoes_atualizado_em ON solicitacoes;
CREATE TRIGGER trigger_solicitacoes_atualizado_em
BEFORE UPDATE ON solicitacoes
FOR EACH ROW
EXECUTE FUNCTION update_atualizado_em();

CREATE OR REPLACE FUNCTION atualizar_media_avaliacoes()
RETURNS TRIGGER AS $$
DECLARE
    prestador_afetado INT;
BEGIN
    IF TG_OP = 'DELETE' THEN
        prestador_afetado := OLD.prestador_id;
    ELSE
        prestador_afetado := NEW.prestador_id;
    END IF;

    UPDATE usuarios
    SET 
        media_avaliacao = COALESCE((
            SELECT AVG(nota)::NUMERIC(3,2)
            FROM avaliacoes
            WHERE prestador_id = prestador_afetado
        ), 0),
        total_avaliacoes = (
            SELECT COUNT(*)
            FROM avaliacoes
            WHERE prestador_id = prestador_afetado
        )
    WHERE id = prestador_afetado;

    IF TG_OP = 'UPDATE' AND OLD.prestador_id IS DISTINCT FROM NEW.prestador_id THEN
        UPDATE usuarios
        SET
            media_avaliacao = COALESCE((
                SELECT AVG(nota)::NUMERIC(3,2)
                FROM avaliacoes
                WHERE prestador_id = OLD.prestador_id
            ), 0),
            total_avaliacoes = (
                SELECT COUNT(*)
                FROM avaliacoes
                WHERE prestador_id = OLD.prestador_id
            )
        WHERE id = OLD.prestador_id;
    END IF;

    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_atualizar_media_avaliacoes ON avaliacoes;
CREATE TRIGGER trigger_atualizar_media_avaliacoes
AFTER INSERT OR UPDATE OR DELETE ON avaliacoes
FOR EACH ROW
EXECUTE FUNCTION atualizar_media_avaliacoes();
