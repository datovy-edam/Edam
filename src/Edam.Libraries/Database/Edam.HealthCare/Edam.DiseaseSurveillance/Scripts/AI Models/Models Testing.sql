CREATE DATABASE db_temp
GO

USE db_temp
GO



-- must enable external AI endpoint support
EXEC sp_configure 'external scripts enabled', 1;
RECONFIGURE;
GO

-- ensure this is enabled
EXEC sp_configure 'external rest endpoint enabled', 1
RECONFIGURE WITH OVERRIDE;
GO

---- turn on Trace Flags for Vector Search
--DBCC TRACEON(466,474,13981,-1)
--GO

---- check trace flags status
--DBCC TraceStatus 
--GO

-- add certificate reference in sql server
-- Create a certificate from file (run in container to get file path)
-- Check trusted certificates in SQL Server
SELECT * FROM sys.certificates WHERE name LIKE '%ollama%';

CREATE CERTIFICATE Ollama_Certificate
FROM FILE = '/etc/ssl/certs/ollama.crt';

-- Create a database scoped credential with the certificate
CREATE DATABASE SCOPED CREDENTIAL Ollama_Credential
WITH IDENTITY = 'OllamaService',
SECRET = 'YourSecurePassword'; -- If certificate is password protected


-- create external model
DROP EXTERNAL MODEL Ollama_Model
GO

CREATE EXTERNAL MODEL Ollama_Model
WITH (
   LOCATION = 'https://192.168.1.74:11435/api/embed',
   API_FORMAT = 'Ollama',
   MODEL_TYPE = EMBEDDINGS,
   MODEL = 'nomic-embed-text',
   CREDENTIAL = Ollama_Credential,
   PARAMETERS = '{"Dimensions": 768}'
);

-- check registered model as follows:
SELECT * FROM sys.external_models;
GO

SELECT AI_GENERATE_EMBEDDINGS (N'This is a test sentence.' USE MODEL Ollama_Model);


EXEC sp_invoke_external_rest_endpoint 
    N'https://api.openai.com/v1/models';


