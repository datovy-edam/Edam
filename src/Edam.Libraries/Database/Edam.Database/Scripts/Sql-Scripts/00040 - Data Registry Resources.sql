
-- -----------------------------------------------------------------------------
-- EDAM — the reference-data REGISTRY rows.
--
-- WHY THIS FILE MATTERS: the schema alone is not a usable database. The procedures the application calls
-- REQUIRE these lookup rows — a domain is inserted with a TypeNo that must exist in Data.DataDomainType, and
-- fk_DataDomainType refuses it otherwise, which is exactly how a fresh deployment failed while editing an
-- arguments file:
--    "The INSERT statement conflicted with the FOREIGN KEY constraint fk_DataDomainType.
--     The conflict occurred in database … table Data.DataDomainType, column 'IdNo'."
--
-- Every block therefore runs ONLY when its table is empty, so this script is SAFE TO RE-RUN — which matters
-- because it is now the database project's POST-DEPLOYMENT script (see Scripts/PostDeployment.sql), so a
-- publish or a Deploy loads the registry instead of leaving it empty.
-- -----------------------------------------------------------------------------
SET NOCOUNT ON

IF NOT EXISTS (SELECT 1 FROM Data.DataKind)
BEGIN
   INSERT INTO Data.DataKind (IdNo, Description, SpanishDescription) VALUES
      (  0, ' ', ' '),
      (  1, 'Property', 'Propiedad'),
      (  2, 'CodeSet', 'Codigos'),
      (  3, 'External Reference', 'Referencia Externa'),
      (  10, 'AuditProperty', 'Auditable')
END
GO

IF NOT EXISTS (SELECT 1 FROM Data.DataKeyType)
BEGIN
   INSERT INTO Data.DataKeyType (IdNo, Description, SpanishDescription) VALUES
      (  0, ' ', ' '),
      (  1, 'None-Key', 'No es Unico'),
      (  2, 'Key', 'Unico'),
      (  3, 'Auto-Generate', 'Auto-Generado'),
      (  4, 'None', 'Ninguno'),
      (  99, 'Undefined', 'Indefinido')
END
GO

IF NOT EXISTS (SELECT 1 FROM Data.DataAssetGroupType)
BEGIN
   INSERT INTO Data.DataAssetGroupType (IdNo, Description, SpanishDescription) VALUES
      (  0, ' ', ' '),
      (  10, 'Catalog', 'Catalogo'),
      (  20, 'Document', 'Documento'),
      (  30, 'Submission', 'Envio')
END
GO

IF NOT EXISTS (SELECT 1 FROM Data.DataElementType)
BEGIN
   INSERT INTO Data.DataElementType (IdNo, Description, SpanishDescription) VALUES
      (  0, ' ', ' '),
      (  1, 'Element', 'Elemento'),
      (  2, 'Attribute', 'Atributo'),
      (  5, 'Type', 'Tipo'),
      (  6, 'Root', 'Raiz'),
      (  7, 'Reference', 'Referencia'),
      (  8, 'Enumerator', 'Enumerador'),
      (  9, 'Key', 'Chave'),
      ( 10, 'AutoPK', 'AutoPK'),
      ( 11, 'Identity', 'Identidad'),
      ( 99, 'Undefined', 'Indefinido')
END
GO

IF NOT EXISTS (SELECT 1 FROM Data.DataGroupType)
BEGIN
   INSERT INTO Data.DataGroupType (IdNo, Description, SpanishDescription) VALUES
      (  0, ' ', ' '),
      (  6, 'Sequence', 'Secuencia'),
      (  7, 'Reference', 'Referencia'),
      (  8, 'OptionAll', 'SeleccionTodos'),
      (  9, 'OptionAny', 'Cualquiera'),
      ( 10, 'OptionOne', 'SeleccionUno'),
      ( 11, 'SubstitutionGroup', 'GrupoSubstitucion'),
      ( 12, 'Template', 'Plantilla')
END
GO

IF NOT EXISTS (SELECT 1 FROM Data.DataConstraintType)
BEGIN
   INSERT INTO Data.DataConstraintType (IdNo, Description, SpanishDescription) VALUES
      (  0, ' ', ' '),
      (  1, 'NonKey', 'Elemento'),
      (  2, 'Key', 'Atributo'),
      (  3, 'AutoPK', 'Tipo'),
      (  4, 'Identity', 'Raiz'),
      ( 99, 'Undefined', 'Indefinido')
END
GO

IF NOT EXISTS (SELECT 1 FROM Data.DataReferenceType)
BEGIN
   INSERT INTO Data.DataReferenceType (IdNo, Description, SpanishDescription) VALUES
      (  0, ' ', ' '),
      (  10, 'Object', 'Objeto'),
      (  20, 'Term', 'Termino'),
      (  30, 'Keyword', 'Keyword'),
      (  40, 'Element', 'Elemento'),
      (  50, 'Attribute', 'Atributo'),
      (  90, 'Table Reference', 'Tabla de Referencia'),
      (  91, 'Table Column Reference', 'Referencia a Tabla de Columna'),
      ( 100, 'URL', 'URL'),
      ( 110, 'Domain URI Prefix', 'Prefijo Dominio URI')
END
GO

IF NOT EXISTS (SELECT 1 FROM Data.DataAssociationType)
BEGIN
   INSERT INTO Data.DataAssociationType (IdNo, Description, SpanishDescription) VALUES
      (  0, ' ', ' '),
      (  10, 'Definition', 'Definicion'),
      (  20, 'Synonymous', 'Sinonimo'),
      (  30, 'Antonymous', 'Antonimo'),
      (  40, 'AKO (A Kind Of)', 'AKO (Tipo de)'),
      (  50, 'Alias', 'Alias')
END
GO

-- the one the domain insert needed: Data.DataDomainType (fk_DataDomainType)
IF NOT EXISTS (SELECT 1 FROM Data.DataDomainType)
BEGIN
   INSERT INTO Data.DataDomainType (IdNo, Description, SpanishDescription) VALUES
      (   0, ' ', ' '),
      (   1, 'Asset', 'Activo'),
      (   2, 'Instance', 'Instancia'),
      (  10, 'Information Exchange', 'Intercambio de Informacion'),
      (  20, 'Form', 'Forma'),
      (  30, 'Use Case', 'Caso de Uso'),
      (  40, 'Code Set', 'Conjunto de Codigos'),
      (  50, 'Document', 'Documento'),
      (  60, 'Report', 'Reporte'),
      (  70, 'Table', 'Table'),
      (  80, 'Schema', 'Esquema')
END
GO

IF NOT EXISTS (SELECT 1 FROM Data.DataReferenceStatus)
BEGIN
   INSERT INTO Data.DataReferenceStatus (IdNo, Description, SpanishDescription) VALUES
      (  0, ' ', ' '),
      (  1, 'Valid', 'Valido/a'),
      (  2, 'Invalid', 'Invalido/a'),
      (  3, 'Obsolete', 'Obsoleto/a'),
      (  4, 'Dismissed', 'Desestimado/a')
END
GO

IF NOT EXISTS (SELECT 1 FROM Data.DataNoteType)
BEGIN
   INSERT INTO Data.DataNoteType (IdNo, Description, SpanishDescription) VALUES
      (  0, ' ', ' '),
      (  10, 'General Note', 'Nota General'),
      (  20, 'Caution - Adivice', 'Recomendacion - Precaucion'),
      (  30, 'Recomendation - Advice', 'Recomendacion - Consejo'),
      (  40, 'Reinforcement - Advice', 'Reforzar - Recomendacion')
END
GO
