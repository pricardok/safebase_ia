-- Ajustes mínimos para chat estilo DeepSeek
-- Adiciona pinned, temperature, max_tokens em chat_conversas

IF COL_LENGTH('dbo.chat_conversas', 'pinned') IS NULL
BEGIN
    ALTER TABLE dbo.chat_conversas
        ADD pinned bit NOT NULL CONSTRAINT DF_chat_conversas_pinned DEFAULT (0);
END

IF COL_LENGTH('dbo.chat_conversas', 'temperature') IS NULL
BEGIN
    ALTER TABLE dbo.chat_conversas
        ADD temperature decimal(4,2) NOT NULL CONSTRAINT DF_chat_conversas_temperature DEFAULT (0.70);
END

IF COL_LENGTH('dbo.chat_conversas', 'max_tokens') IS NULL
BEGIN
    ALTER TABLE dbo.chat_conversas
        ADD max_tokens int NOT NULL CONSTRAINT DF_chat_conversas_max_tokens DEFAULT (2000);
END
