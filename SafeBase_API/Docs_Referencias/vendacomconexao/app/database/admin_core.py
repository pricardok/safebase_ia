# app/database/admin_core.py
import logging
from typing import List, Dict, Any, Optional
from psycopg2.extras import DictCursor
from app.database.core import get_db_connection
import json
import uuid
from datetime import datetime, timezone
from ..models_admin import GlobalDiscountCreate, GlobalDiscountUpdate

logger = logging.getLogger(__name__)

def get_all_clientes_admin(
    limit: int = 100,
    offset: int = 0,
    search: Optional[str] = None,
    status: Optional[str] = None) -> List[Dict[str, Any]]:
    """
    Busca todos os clientes (tenants) do sistema para o painel de administração,
    com suporte a paginação, busca e filtro por status.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:                
                # Passo 1: Obter os IDs dos clientes que correspondem aos filtros e à paginação.
                # Isso garante que a paginação funcione corretamente no nível do cliente.
                id_query_parts = ["SELECT c.id FROM clientes c"]
                id_conditions = []
                id_params = []

                if search:
                    search_term = f"%{search}%"
                    id_conditions.append("(c.razao_social ILIKE %s OR c.email_contato ILIKE %s OR c.documento ILIKE %s)")
                    id_params.extend([search_term, search_term, search_term])

                if status:
                    id_conditions.append("c.status = %s")
                    id_params.append(status)

                if id_conditions:
                    id_query_parts.append("WHERE " + " AND ".join(id_conditions))

                id_query_parts.append("ORDER BY c.created_at DESC LIMIT %s OFFSET %s")
                id_params.extend([limit, offset])
                
                id_query = " ".join(id_query_parts)
                cur.execute(id_query, id_params)
                
                cliente_ids = [row['id'] for row in cur.fetchall()]
                if not cliente_ids:
                    return []

                # Passo 2: Executar a query completa para os IDs de cliente selecionados.
                main_query = """
                    SELECT 
                        c.id as cliente_id, c.razao_social, c.nome_fantasia, c.documento,
                        c.status as cliente_status, c.created_at as cliente_criado_em,
                        c.plano_id, c.trial_expires_at, c.email_contato, c.telefone, c.expiry_date,
                        case 
                            when c.tipo = 'J' then 'Pessoa Jurídica'
                            when c.tipo = 'F' then 'Pessoa Física'
                        end as cliente_tipo,
                        u.id as user_id, u.username, u.email as user_email, u.full_name,
                        u.is_active as user_ativo, u.created_at as user_criado_em, u.must_change_password,
                        p.id as perfil_id, p.nome as perfil_nome, p.descricao as perfil_descricao,
                        pl.id as plano_id, pl.nome as plano_nome, pl.descricao as plano_descricao,
                        pl.tipo as plano_tipo, pl.preco_mensal, pl.max_clientes, pl.max_usuarios,
                        pl.features as plano_features, pl.ativo as plano_ativo,
                        dc.id as desconto_id, dc.tipo as desconto_tipo, dc.valor as desconto_valor,
                        dc.expira_em as desconto_expira_em, dc.ativo as desconto_ativo
                    FROM clientes c
                    LEFT JOIN users u ON u.cliente_id = c.id
                    LEFT JOIN usuario_perfis up ON up.usuario_id = u.id
                    LEFT JOIN perfis p ON p.id = up.perfil_id
                    LEFT JOIN planos pl ON pl.id = c.plano_id
                    LEFT JOIN descontos_cliente dc ON dc.cliente_id = c.id
                    WHERE c.id = ANY(%s::uuid[]) -- Correção: Cast explícito para array de UUIDs
                    ORDER BY c.created_at DESC, u.created_at ASC;
                """
                cur.execute(main_query, (cliente_ids,))
                rows = cur.fetchall()

                # Passo 3: Processar e agrupar os resultados em Python.
                clientes_map = {}
                for row in rows:
                    cliente_id = row['cliente_id']
                    if cliente_id not in clientes_map:
                        clientes_map[cliente_id] = {
                            "cliente_id": cliente_id, "razao_social": row['razao_social'],
                            "nome_fantasia": row['nome_fantasia'], "documento": row['documento'],
                            "cliente_status": row['cliente_status'], "cliente_criado_em": row['cliente_criado_em'],
                            "trial_expires_at": row['trial_expires_at'], "email_contato": row['email_contato'],
                            "telefone": row['telefone'], "cliente_tipo": row['cliente_tipo'],
                            "expiry_date": row['expiry_date'],
                            "plano": {
                                "plano_id": row['plano_id'], "plano_nome": row['plano_nome'],
                                "plano_descricao": row['plano_descricao'], "plano_tipo": row['plano_tipo'],
                                "preco_mensal": row['preco_mensal'], "max_clientes": row['max_clientes'],
                                "max_usuarios": row['max_usuarios'], "plano_features": row['plano_features'],
                                "plano_ativo": row['plano_ativo']
                            },
                            "usuarios": [],
                            "descontos": []
                        }

                    # Adicionar usuário se ele existir e não tiver sido adicionado
                    if row['user_id'] and not any(u['user_id'] == row['user_id'] for u in clientes_map[cliente_id]['usuarios']):
                        clientes_map[cliente_id]['usuarios'].append({
                            "user_id": row['user_id'], "username": row['username'], "user_email": row['user_email'],
                            "full_name": row['full_name'], "user_ativo": row['user_ativo'],
                            "user_criado_em": row['user_criado_em'], "must_change_password": row['must_change_password'],
                            "perfil_id": row['perfil_id'], "perfil_nome": row['perfil_nome'],
                            "perfil_descricao": row['perfil_descricao']
                        })

                    # Adicionar desconto se ele existir e não tiver sido adicionado
                    if row['desconto_id'] and not any(d['desconto_id'] == row['desconto_id'] for d in clientes_map[cliente_id]['descontos']):
                        clientes_map[cliente_id]['descontos'].append({
                            "desconto_id": row['desconto_id'], "desconto_tipo": row['desconto_tipo'],
                            "desconto_valor": row['desconto_valor'], "desconto_expira_em": row['desconto_expira_em'],
                            "desconto_ativo": row['desconto_ativo']
                        })

                return list(clientes_map.values())

    except Exception as e:
        logger.error(f"Erro ao buscar todos os clientes para admin com a nova query: {e}", exc_info=True)
        return []

def get_perfis_detalhados_admin(search: Optional[str] = None) -> List[Dict[str, Any]]:
    """
    Busca todos os perfis, seus módulos, permissões e contagem de usuários.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                query = """
                    SELECT 
                        p.id as perfil_id, p.nome as perfil_nome, p.descricao as perfil_descricao,
                        m.id as modulo_id, m.nome as modulo_nome,
                        m.endpoint_base as modulo_endpoint, m.descricao as modulo_descricao,
                        pm.pode_acessar as permissao_acesso,
                        (SELECT COUNT(up.usuario_id) FROM usuario_perfis up WHERE up.perfil_id = p.id) as total_usuarios
                    FROM perfis p
                    LEFT JOIN perfil_modulos pm ON pm.perfil_id = p.id
                    LEFT JOIN modulos m ON m.id = pm.modulo_id
                """
                params = []
                if search:
                    query += " WHERE p.nome ILIKE %s"
                    params.append(f"%{search}%")
                
                query += " ORDER BY p.nome, m.nome;"
                cur.execute(query, params)
                rows = cur.fetchall()

                perfis_map = {}
                for row in rows:
                    perfil_id = row['perfil_id']
                    if perfil_id not in perfis_map:
                        perfis_map[perfil_id] = {
                            "perfil_id": perfil_id, "perfil_nome": row['perfil_nome'],
                            "perfil_descricao": row['perfil_descricao'],
                            "total_usuarios": int(row['total_usuarios']),
                            "modulos": []
                        }
                    if row['modulo_id']:
                        perfis_map[perfil_id]['modulos'].append({
                            "modulo_id": row['modulo_id'], "modulo_nome": row['modulo_nome'],
                            "modulo_endpoint": row['modulo_endpoint'], "modulo_descricao": row['modulo_descricao'],
                            "permissao_acesso": row['permissao_acesso']
                        })
                return list(perfis_map.values())
    except Exception as e:
        logger.error(f"Erro ao buscar perfis detalhados para admin: {e}", exc_info=True)
        return []

def get_usuarios_com_perfis_admin(limit: int, offset: int, search: Optional[str] = None, perfil_id: Optional[int] = None) -> List[Dict[str, Any]]:
    """
    Busca usuários com seus perfis e módulos acessíveis.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                query = """
                    SELECT 
                        u.id as user_id, u.username, u.email as user_email, u.full_name, u.is_active as user_ativo,
                        c.razao_social as cliente_razao_social, p.nome as perfil_nome,
                        STRING_AGG(CASE WHEN pm.pode_acessar THEN m.nome ELSE NULL END, ', ' ) as modulos_com_acesso
                    FROM users u
                    LEFT JOIN clientes c ON c.id = u.cliente_id
                    LEFT JOIN usuario_perfis up ON up.usuario_id = u.id
                    LEFT JOIN perfis p ON p.id = up.perfil_id
                    LEFT JOIN perfil_modulos pm ON pm.perfil_id = p.id
                    LEFT JOIN modulos m ON m.id = pm.modulo_id
                """
                conditions = []
                params = []
                if search:
                    search_term = f"%{search}%"
                    conditions.append("(u.username ILIKE %s OR u.email ILIKE %s OR u.full_name ILIKE %s OR c.razao_social ILIKE %s)")
                    params.extend([search_term, search_term, search_term, search_term])
                if perfil_id:
                    conditions.append("p.id = %s")
                    params.append(perfil_id)

                if conditions:
                    query += " WHERE " + " AND ".join(conditions)

                query += """
                    GROUP BY u.id, u.username, u.email, u.full_name, u.is_active, c.razao_social, p.nome
                    ORDER BY c.razao_social, u.username
                    LIMIT %s OFFSET %s;
                """
                params.extend([limit, offset])
                cur.execute(query, params)
                return [dict(row) for row in cur.fetchall()]
    except Exception as e:
        logger.error(f"Erro ao buscar usuários com perfis para admin: {e}", exc_info=True)
        return []

def get_modulos_disponiveis_admin() -> List[Dict[str, Any]]:
    """
    Busca todos os módulos do sistema e os perfis que têm acesso.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                query = """
                    SELECT 
                        m.id, m.nome as nome_modulo, m.endpoint_base, m.descricao,
                        COUNT(DISTINCT CASE WHEN pm.pode_acessar THEN pm.perfil_id ELSE NULL END) as total_perfis_com_acesso,
                        STRING_AGG(DISTINCT CASE WHEN pm.pode_acessar THEN p.nome ELSE NULL END, ', ') as perfis_com_acesso
                    FROM modulos m
                    LEFT JOIN perfil_modulos pm ON pm.modulo_id = m.id
                    LEFT JOIN perfis p ON p.id = pm.perfil_id
                    GROUP BY m.id, m.nome, m.endpoint_base, m.descricao
                    ;
                """
                cur.execute(query)
                return [dict(row) for row in cur.fetchall()]
    except Exception as e:
        logger.error(f"Erro ao buscar módulos disponíveis para admin: {e}", exc_info=True)
        return []

def get_permissoes_detalhadas_por_cliente_admin(limit: int, offset: int, cliente_id: Optional[str] = None, search_user: Optional[str] = None) -> List[Dict[str, Any]]:
    """
    Busca permissões detalhadas por cliente e usuário.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                query = """
                    SELECT 
                        c.id as cliente_id, c.razao_social, u.id as user_id, u.username, p.nome as perfil_nome,
                        m.nome as modulo_nome, pm.pode_acessar as permissao
                    FROM clientes c
                    JOIN users u ON u.cliente_id = c.id
                    JOIN usuario_perfis up ON up.usuario_id = u.id
                    JOIN perfis p ON p.id = up.perfil_id
                    JOIN perfil_modulos pm ON pm.perfil_id = p.id
                    JOIN modulos m ON m.id = pm.modulo_id
                """
                conditions = ["c.status = 'ativo'", "u.is_active = true"]
                params = []
                if cliente_id:
                    conditions.append("c.id = %s")
                    params.append(cliente_id)
                if search_user:
                    search_term = f"%{search_user}%"
                    conditions.append("(u.username ILIKE %s OR u.email ILIKE %s)")
                    params.extend([search_term, search_term])

                if conditions:
                    query += " WHERE " + " AND ".join(conditions)

                query += " ORDER BY c.razao_social, u.username, m.nome LIMIT %s OFFSET %s;"
                params.extend([limit, offset])
                cur.execute(query, params)
                
                # Agrupamento em Python
                clientes_map = {}
                for row in cur.fetchall():
                    cid = row['cliente_id']
                    if cid not in clientes_map:
                        clientes_map[cid] = {"cliente_id": cid, "razao_social": row['razao_social'], "usuarios": {}}
                    
                    uid = row['user_id']
                    if uid not in clientes_map[cid]['usuarios']:
                        clientes_map[cid]['usuarios'][uid] = {"user_id": uid, "username": row['username'], "perfil_nome": row['perfil_nome'], "permissoes": []}
                    
                    clientes_map[cid]['usuarios'][uid]['permissoes'].append({
                        "modulo": row['modulo_nome'],
                        "permitido": row['permissao']
                    })
                
                # Converte o mapa aninhado em uma lista de resultados
                resultado_final = []
                for cid, cdata in clientes_map.items():
                    cdata['usuarios'] = list(cdata['usuarios'].values())
                    resultado_final.append(cdata)

                return resultado_final
    except Exception as e:
        logger.error(f"Erro ao buscar permissões detalhadas por cliente: {e}", exc_info=True)
        return []

def get_auditoria_acessos_perfis_admin() -> List[Dict[str, Any]]:
    """
    Busca um resumo de auditoria de acessos por perfil.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                query = """
                    SELECT 
                        p.nome as perfil_nome,
                        COUNT(DISTINCT up.usuario_id) as total_usuarios,
                        SUM(CASE WHEN pm.pode_acessar THEN 1 ELSE 0 END) as modulos_com_acesso,
                        STRING_AGG(DISTINCT CASE WHEN pm.pode_acessar THEN m.nome ELSE NULL END, ', ') as modulos_permitidos
                    FROM perfis p
                    LEFT JOIN usuario_perfis up ON up.perfil_id = p.id
                    LEFT JOIN perfil_modulos pm ON pm.perfil_id = p.id
                    LEFT JOIN modulos m ON m.id = pm.modulo_id
                    GROUP BY p.id, p.nome
                    ORDER BY p.nome;
                """
                cur.execute(query)
                return [dict(row) for row in cur.fetchall()]
    except Exception as e:
        logger.error(f"Erro ao buscar auditoria de acessos por perfil: {e}", exc_info=True)
        return []

def get_all_global_discounts_admin() -> List[Dict[str, Any]]:
    """Busca todos os descontos globais (cupons) do sistema."""
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                cur.execute("""
                    SELECT id, codigo, tipo, valor, max_usos, usos_atuais, expira_em, ativo, criado_em
                    FROM descontos_globais
                    ORDER BY criado_em DESC;
                """)
                return [dict(row) for row in cur.fetchall()]
    except Exception as e:
        logger.error(f"Erro ao buscar descontos globais: {e}", exc_info=True)
        return []

def create_global_discount_admin(discount_data: 'GlobalDiscountCreate') -> Optional[Dict[str, Any]]:
    """Cria um novo desconto global (cupom)."""
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                cur.execute("""
                    INSERT INTO descontos_globais (codigo, tipo, valor, max_usos, expira_em, ativo)
                    VALUES (%(codigo)s, %(tipo)s, %(valor)s, %(max_usos)s, %(expira_em)s, %(ativo)s)
                    RETURNING *;
                """, discount_data.model_dump())
                new_discount = cur.fetchone()
                conn.commit()
                return dict(new_discount) if new_discount else None
    except Exception as e:
        logger.error(f"Erro ao criar desconto global: {e}", exc_info=True)
        return None

def update_global_discount_admin(discount_id: int, discount_data: 'GlobalDiscountUpdate') -> Optional[Dict[str, Any]]:
    """Atualiza um desconto global (cupom) existente."""
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                update_dict = discount_data.model_dump()
                update_dict['id'] = discount_id
                cur.execute("""
                    UPDATE descontos_globais
                    SET codigo = %(codigo)s, tipo = %(tipo)s, valor = %(valor)s,
                        max_usos = %(max_usos)s, expira_em = %(expira_em)s, ativo = %(ativo)s,
                        atualizado_em = NOW()
                    WHERE id = %(id)s
                    RETURNING *;
                """, update_dict)
                updated_discount = cur.fetchone()
                conn.commit()
                return dict(updated_discount) if updated_discount else None
    except Exception as e:
        logger.error(f"Erro ao atualizar desconto global {discount_id}: {e}", exc_info=True)
        return None

def delete_global_discount_admin(discount_id: int) -> bool:
    """Exclui um desconto global (cupom)."""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("DELETE FROM descontos_globais WHERE id = %s", (discount_id,))
                conn.commit()
                return cur.rowcount > 0
    except Exception as e:
        logger.error(f"Erro ao excluir desconto global {discount_id}: {e}", exc_info=True)
        return False

def apply_discount_to_client_admin(cliente_id: str, codigo_cupom: str, expira_em: Optional[str]) -> Optional[Dict[str, Any]]:
    """Aplica um desconto global a um cliente específico."""
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                # 1. Encontrar o cupom global
                cur.execute("""
                    SELECT id, tipo, valor, expira_em as cupom_expira_em, max_usos, usos_atuais, ativo
                    FROM descontos_globais
                    WHERE codigo = %s AND ativo = TRUE;
                """, (codigo_cupom,))
                cupom = cur.fetchone()

                if not cupom:
                    logger.warning(f"Tentativa de aplicar cupom inexistente ou inativo: {codigo_cupom}")
                    return None

                # 2. Verificar limites de uso
                if cupom['max_usos'] > 0 and cupom['usos_atuais'] >= cupom['max_usos']:
                    logger.warning(f"Cupom {codigo_cupom} atingiu o limite máximo de usos.")
                    return None

                # 3. Obter o plano atual do cliente para associar o desconto
                cur.execute("SELECT plano_id FROM clientes WHERE id = %s", (cliente_id,))
                plano_row = cur.fetchone()
                if not plano_row or not plano_row['plano_id']:
                    logger.error(f"Cliente {cliente_id} não encontrado ou sem plano associado.")
                    return None
                plano_id = plano_row['plano_id']

                # 4. Inserir ou atualizar o desconto para o cliente na tabela `descontos_cliente`
                data_expiracao_final = expira_em or cupom['cupom_expira_em']
                cur.execute("""
                    INSERT INTO descontos_cliente (cliente_id, plano_id, tipo, valor, expira_em, descricao, ativo)
                    VALUES (%s, %s, %s, %s, %s, %s, TRUE)
                    ON CONFLICT (cliente_id, plano_id) DO UPDATE SET
                        tipo = EXCLUDED.tipo, valor = EXCLUDED.valor, expira_em = EXCLUDED.expira_em,
                        descricao = EXCLUDED.descricao, ativo = TRUE, updated_at = NOW()
                    RETURNING id, tipo, valor, expira_em, ativo;
                """, (cliente_id, plano_id, cupom['tipo'], cupom['valor'], data_expiracao_final, f"Aplicado via cupom: {codigo_cupom}"))
                desconto_aplicado = cur.fetchone()

                # 5. Incrementar o contador de usos do cupom global
                cur.execute("UPDATE descontos_globais SET usos_atuais = usos_atuais + 1 WHERE id = %s", (cupom['id'],))

                conn.commit()
                
                return dict(desconto_aplicado) if desconto_aplicado else None
    except Exception as e:
        logger.error(f"Erro ao aplicar desconto ao cliente {cliente_id}: {e}", exc_info=True)
        conn.rollback()
        return None
