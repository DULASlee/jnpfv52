import { runSqlQuery } from './lib/jnpf-db.mjs';

for (const id of ['355', '356', '354', '353']) {
  const msgs = await runSqlQuery(`
SET NOCOUNT ON;
SELECT COUNT(*) AS Cnt,
  SUM(CASE WHEN F_ROLE='user' THEN 1 ELSE 0 END) AS Users,
  SUM(CASE WHEN F_ROLE='assistant' THEN 1 ELSE 0 END) AS Assts,
  SUM(CASE WHEN F_ROLE='system' THEN 1 ELSE 0 END) AS Sys,
  MAX(LEN(ISNULL(F_CONTENT,''))) AS MaxLen
FROM BASE_AI_PIPELINE_MESSAGE
WHERE F_PIPELINE_ID='${id}' AND (F_DELETE_MARK IS NULL OR F_DELETE_MARK=0);
`);
  const att = await runSqlQuery(`
SET NOCOUNT ON;
SELECT COUNT(*) AS Cnt,
  SUM(CASE WHEN F_PROCESS_STATUS=2 THEN 1 ELSE 0 END) AS Ok,
  MAX(LEN(ISNULL(F_EXTRACTED_TEXT,''))) AS MaxExtract
FROM inte_assistant_attachment
WHERE F_PIPELINE_ID='${id}' AND (F_DELETE_MARK IS NULL OR F_DELETE_MARK=0);
`);
  const del = await runSqlQuery(`
SET NOCOUNT ON;
SELECT COUNT(*) AS Cnt, LEFT(STRING_AGG(CAST(F_FILE_NAME AS NVARCHAR(200)), ','), 300) AS Names
FROM BASE_AI_GENERATED_FILE
WHERE F_PIPELINE_ID='${id}' OR F_PIPELINE_ID=N'${id}';
`);
  console.log(`\n=== pipeline ${id} ===`);
  console.log('messages', msgs);
  console.log('attachments', att);
  console.log('files', del);
}
