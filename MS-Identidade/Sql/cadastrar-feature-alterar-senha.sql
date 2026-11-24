-- Executar no banco de cada empresa, após o cadastro da feature USUARIO.
INSERT INTO identidade.features (id_feature_pai, codigo, nome, ativo)
SELECT pai.id, 'USUARIO_ALTERAR_SENHA', 'Alterar senha de usuários', TRUE
FROM identidade.features AS pai
WHERE pai.codigo = 'USUARIO'
ON CONFLICT (codigo) DO NOTHING;
