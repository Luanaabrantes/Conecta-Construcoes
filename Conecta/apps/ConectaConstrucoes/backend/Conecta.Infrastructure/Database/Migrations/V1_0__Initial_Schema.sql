-- Script Conecta (PostgreSQL)

CREATE TABLE usuarios (
    id SERIAL PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    senha_hash VARCHAR(255) NOT NULL,
    perfil VARCHAR(20) NOT NULL, -- 'PRESTADOR', 'CLIENTE', 'ADMIN'
    ativo BOOLEAN DEFAULT TRUE,
    documento VARCHAR(20),
    verificado BOOLEAN DEFAULT FALSE,
    foto_perfil_url VARCHAR(255),
    media_avaliacao DECIMAL(3,2) DEFAULT 0.00,
    total_avaliacoes INT DEFAULT 0,
    termos_aceitos_em TIMESTAMP,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE telefones (
    id SERIAL PRIMARY KEY,
    usuario_id INT NOT NULL,
    numero VARCHAR(20) NOT NULL,
    principal BOOLEAN DEFAULT FALSE,
    tipo VARCHAR(20) DEFAULT 'WHATSAPP',
    CONSTRAINT fk_telefones_user FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);

-- Nova Tabela de Estados
CREATE TABLE estados (
    id SERIAL PRIMARY KEY,
    sigla CHAR(2) UNIQUE NOT NULL, -- Ex: 'SP'
    nome VARCHAR(50) NOT NULL      -- Ex: 'São Paulo'
);

-- Tabela Cidades agora ligada a Estados
CREATE TABLE cidades (
    id SERIAL PRIMARY KEY,
    estado_id INT NOT NULL,
    nome VARCHAR(100) NOT NULL,
    CONSTRAINT fk_cidades_estado FOREIGN KEY (estado_id) REFERENCES estados(id)
);

CREATE TABLE enderecos (
    id SERIAL PRIMARY KEY,
    usuario_id INT NOT NULL,
    cidade_id INT NOT NULL,
    bairro VARCHAR(100),
    cep VARCHAR(20),
    latitude DECIMAL(9,6),
    longitude DECIMAL(9,6),
    principal BOOLEAN DEFAULT TRUE,
    CONSTRAINT fk_enderecos_user FOREIGN KEY (usuario_id) REFERENCES usuarios(id),
    CONSTRAINT fk_enderecos_cidade FOREIGN KEY (cidade_id) REFERENCES cidades(id)
);

CREATE TABLE clientes (
    usuario_id INT PRIMARY KEY, 
    nome_completo VARCHAR(100) NOT NULL,
    CONSTRAINT fk_cliente_user FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);

CREATE TABLE prestadores (
    usuario_id INT PRIMARY KEY,
    nome_fantasia VARCHAR(100) NOT NULL,
    bio TEXT,
    plano_assinatura CHAR(1) DEFAULT 'G', -- 'G' para Grátis, 'P' para Premium
    status_disponibilidade VARCHAR(20) DEFAULT 'DISPONIVEL',
    CONSTRAINT fk_prestador_user FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);

CREATE TABLE categorias (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    slug VARCHAR(100) UNIQUE NOT NULL, -- URL (Ex: 'reforma-e-reparos')
    descricao TEXT,
    icone_url VARCHAR(255)
);

CREATE TABLE prestador_categorias (
    prestador_id INT NOT NULL,
    categoria_id INT NOT NULL,
    PRIMARY KEY (prestador_id, categoria_id),
    CONSTRAINT fk_pc_prestador FOREIGN KEY (prestador_id) REFERENCES prestadores(usuario_id),
    CONSTRAINT fk_pc_categoria FOREIGN KEY (categoria_id) REFERENCES categorias(id)
);

CREATE TABLE portfolio_items (
    id SERIAL PRIMARY KEY,
    prestador_id INT NOT NULL,
    imagem_url VARCHAR(255) NOT NULL,
    titulo VARCHAR(100),
    descricao TEXT,
    tags_ia TEXT,          -- Reservado para IA futura
    analise_ia_json TEXT,  -- Reservado para IA futura
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_portfolio_prestador FOREIGN KEY (prestador_id) REFERENCES prestadores(usuario_id)
);

CREATE TABLE pedidos (
    id SERIAL PRIMARY KEY,
    cliente_id INT NOT NULL,
    prestador_id INT NOT NULL,
    descricao_necessidade TEXT NOT NULL,
    status VARCHAR(20) DEFAULT 'PENDENTE', -- 'PENDENTE', 'ACEITO', 'RECUSADO', 'CONCLUIDO'
    valor_combinado DECIMAL(10,2),
    data_solicitacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_conclusao TIMESTAMP,
    CONSTRAINT fk_pedido_cliente FOREIGN KEY (cliente_id) REFERENCES clientes(usuario_id),
    CONSTRAINT fk_pedido_prestador FOREIGN KEY (prestador_id) REFERENCES prestadores(usuario_id)
);

CREATE TABLE avaliacoes (
    id SERIAL PRIMARY KEY,
    pedido_id INT UNIQUE NOT NULL, -- 1 avaliação por pedido
    cliente_id INT NOT NULL,
    prestador_id INT NOT NULL,
    nota INT NOT NULL CHECK (nota >= 1 AND nota <= 5),
    comentario TEXT,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_aval_pedido FOREIGN KEY (pedido_id) REFERENCES pedidos(id),
    CONSTRAINT fk_aval_cliente FOREIGN KEY (cliente_id) REFERENCES clientes(usuario_id),
    CONSTRAINT fk_aval_prestador FOREIGN KEY (prestador_id) REFERENCES prestadores(usuario_id)
);