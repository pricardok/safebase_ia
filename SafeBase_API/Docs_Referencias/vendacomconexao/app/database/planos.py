import logging
from typing import List, Dict, Any, Optional, Tuple
from datetime import datetime, timedelta
import psycopg2.extras
from .core import get_db_connection
import time
import json

logger = logging.getLogger(__name__)

# Cache para queries frequentes
_planos_cache = None
_planos_cache_time = 0
_PLANOS_CACHE_TTL = 300  # 5 minutos

_planos_publicos_cache = None
_planos_publicos_cache_time = 0
_PLANOS_PUBLICOS_CACHE_TTL = 3600  # 1 hora

_plano_trial_cache = None
_plano_trial_cache_time = 0
_PLANO_TRIAL_CACHE_TTL = 1800  # 30 minutos

def create_cliente(razao_social: str, nome_fantasia: str, documento: str, tipo_pessoa: str, email_contato: str, telefone: str, plano_id: int, trial_dias: int = 7) -> str:
    """
    Cria um novo cliente (tenant) no sistema.
    
    Args:
        razao_social: Razão social do cliente.
        nome_fantasia: Nome fantasia do cliente.
        documento: CNPJ ou CPF do cliente.
        tipo_pessoa: 'F' para Física, 'J' para Jurídica.
        email_contato: Email principal de contato.
        telefone: Telefone de contato.
        plano_id: ID do plano inicial.
        trial_dias: Duração do período de trial em dias.

    Returns:
        O ID (UUID) do cliente criado.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(f"""
                    INSERT INTO clientes (razao_social, nome_fantasia, documento, tipo, email_contato, telefone, plano_id, status)
                    VALUES (%s, %s, %s, %s, %s, %s, %s, 'ativo')
                    RETURNING id
                """, (razao_social, nome_fantasia, documento, tipo_pessoa, email_contato, telefone, plano_id))
                
                cliente_id = cur.fetchone()[0]
                conn.commit()
                logger.info(f"✅ Cliente '{razao_social}' criado com sucesso. ID: {cliente_id}")
                return cliente_id
    except Exception as e:
        logger.error(f"Erro ao criar cliente '{razao_social}': {e}")
        raise

def get_planos_publicos():
    """Obtém lista de planos para exibição pública - OTIMIZADO COM CACHE"""
    global _planos_publicos_cache, _planos_publicos_cache_time
    
    # Verificar cache
    current_time = time.time()
    if (_planos_publicos_cache is not None and 
        current_time - _planos_publicos_cache_time < _PLANOS_PUBLICOS_CACHE_TTL):
        return _planos_publicos_cache
    
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT id, nome, descricao, preco_mensal, max_clientes, max_usuarios, features, ordem_exibicao
                    FROM planos 
                    WHERE ativo = true AND tipo != 'trial'
                    ORDER BY ordem_exibicao, preco_mensal
                """)
                
                planos = []
                for row in cur.fetchall():
                    planos.append({
                        "id": row[0],
                        "nome": row[1],
                        "descricao": row[2],
                        "preco_mensal": float(row[3]) if row[3] else 0.0,
                        "max_clientes": row[4],
                        "max_usuarios": row[5],
                        "features": row[6] or {},
                        "ordem_exibicao": row[7]
                    })
                
                # Armazenar em cache
                _planos_publicos_cache = planos
                _planos_publicos_cache_time = current_time
                
                return planos
                
    except Exception as e:
        logger.error(f"Erro ao buscar planos públicos: {e}")
        return []

def get_all_planos_for_admin() -> List[Dict[str, Any]]:
    """
    Obtém todos os planos do sistema, formatados para o painel de administração,
    garantindo a compatibilidade com a interface do frontend.
    """
    try:
        with get_db_connection() as conn:
            # Usando DictCursor para facilitar o acesso aos campos por nome
            with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cur:
                cur.execute("""
                    SELECT id, nome, descricao, preco_mensal, max_usuarios, ativo
                    FROM planos
                    ORDER BY ordem_exibicao, preco_mensal
                """)
                
                planos_formatados = []
                for row in cur.fetchall():
                    planos_formatados.append({
                        "id": row["id"],
                        "nome": row["nome"] or "Plano sem nome",
                        # CORREÇÃO: Renomeado de 'preco' para 'preco_mensal' para alinhar com a interface do frontend.
                        "preco_mensal": float(row["preco_mensal"]) if row["preco_mensal"] is not None else 0.0,
                        # Garante que a descrição seja sempre uma string
                        "descricao": row["descricao"] or "Sem descrição disponível.",
                        "max_usuarios": row["max_usuarios"] or 0,
                        "ativo": row["ativo"]
                    })
                return planos_formatados
    except Exception as e:
        logger.error(f"Erro ao buscar todos os planos para admin: {e}")
        return []

def get_plano_trial():
    """Obtém o plano trial padrão - OTIMIZADO COM CACHE"""
    global _plano_trial_cache, _plano_trial_cache_time
    
    # Verificar cache
    current_time = time.time()
    if (_plano_trial_cache is not None and 
        current_time - _plano_trial_cache_time < _PLANO_TRIAL_CACHE_TTL):
        return _plano_trial_cache
    
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT id, nome, descricao, tipo, preco_mensal, max_clientes, max_usuarios, features
                    FROM planos 
                    WHERE tipo = 'trial' AND ativo = true
                    LIMIT 1
                """)
                
                result = cur.fetchone()
                if result:
                    plano_trial = {
                        "id": result[0],
                        "nome": result[1],
                        "descricao": result[2],
                        "tipo": result[3],
                        "preco_mensal": float(result[4]) if result[4] else 0.0,
                        "max_clientes": result[5],
                        "max_usuarios": result[6],
                        "features": result[7] or {}
                    }
                    
                    # Armazenar em cache
                    _plano_trial_cache = plano_trial
                    _plano_trial_cache_time = current_time
                    
                    return plano_trial
        
        return None
        
    except Exception as e:
        logger.error(f"Erro ao buscar plano trial: {e}")
        return None

def get_plano_inicial_padrao():
    """
    Obtém o primeiro plano 'pago' disponível, ordenado pela ordem de exibição.
    Este será o plano padrão para novos clientes.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT id, nome, descricao, tipo, preco_mensal, max_clientes, max_usuarios, features
                    FROM planos 
                    WHERE tipo = 'pago' AND ativo = true
                    ORDER BY ordem_exibicao, preco_mensal
                    LIMIT 1
                """)
                
                result = cur.fetchone()
                if result:
                    plano_padrao = {
                        "id": result[0],
                        "nome": result[1],
                        "descricao": result[2],
                        "tipo": result[3],
                        "preco_mensal": float(result[4]) if result[4] else 0.0,
                        "max_clientes": result[5],
                        "max_usuarios": result[6],
                        "features": result[7] or {}
                    }
                    return plano_padrao
        
        logger.warning("Nenhum plano inicial padrão (pago e ativo) foi encontrado.")
        return None
        
    except Exception as e:
        logger.error(f"Erro ao buscar plano inicial padrão: {e}")
        return None

def get_plano_by_id(plano_id: int):
    """Obtém um plano específico por ID - OTIMIZADO"""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT id, nome, descricao, tipo, preco_mensal, max_clientes, max_usuarios, features, ativo
                    FROM planos 
                    WHERE id = %s
                """, (plano_id,))
                
                result = cur.fetchone()
                if result:
                    return {
                        "id": result[0],
                        "nome": result[1],
                        "descricao": result[2],
                        "tipo": result[3],
                        "preco_mensal": float(result[4]) if result[4] else 0.0,
                        "max_clientes": result[5],
                        "max_usuarios": result[6],
                        "features": result[7] or {},
                        "ativo": result[8]
                    }
        return None
        
    except Exception as e:
        logger.error(f"Erro ao buscar plano {plano_id}: {e}")
        return None

def get_cliente_by_id(cliente_id: str) -> Optional[Dict[str, Any]]:
    """Obtém os dados de um cliente específico por ID."""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT id, razao_social, nome_fantasia, status, whatsapp_webhooks_enabled, telefone, email_contato
                    FROM clientes
                    WHERE id = %s
                """, (cliente_id,))
                
                result = cur.fetchone()
                if result:
                    return {
                        "id": result[0],
                        "razao_social": result[1],
                        "nome_fantasia": result[2],
                        "status": result[3],
                        "whatsapp_webhooks_enabled": result[4],
                        "telefone": result[5],
                        "email_contato": result[6]
                    }
        return None
    except Exception as e:
        logger.error(f"Erro ao buscar cliente {cliente_id}: {e}")
        return None

def get_cliente_by_email(email: str) -> Optional[Dict[str, Any]]:
    """Busca um cliente pelo seu email de contato."""
    try:
        with get_db_connection() as conn:
            # Usando DictCursor para facilitar o acesso aos campos por nome
            with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cur:
                cur.execute("SELECT * FROM clientes WHERE email_contato = %s", (email,))
                cliente = cur.fetchone()
                # Converte o resultado em um dicionário padrão antes de retornar
                return dict(cliente) if cliente else None
    except Exception as e:
        logger.error(f"Erro ao buscar cliente por email '{email}': {e}")
        return None

def get_cliente_by_email(email: str) -> Optional[Dict[str, Any]]:
    """Busca um cliente pelo seu email de contato."""
    try:
        with get_db_connection() as conn:
            # Usando DictCursor para facilitar o acesso aos campos por nome
            with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cur:
                cur.execute("SELECT * FROM clientes WHERE email_contato = %s", (email,))
                cliente = cur.fetchone()
                # Converte o resultado em um dicionário padrão antes de retornar
                return dict(cliente) if cliente else None
    except Exception as e:
        logger.error(f"Erro ao buscar cliente por email '{email}': {e}")
        return None

def get_plano_do_cliente(cliente_id: str):
    """Obtém o plano atual de um cliente com desconto aplicado - OTIMIZADO"""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                # Buscar cliente com plano
                cur.execute("""
                    SELECT c.plano_id, p.nome, p.descricao, p.tipo, p.preco_mensal,
                           p.max_clientes, p.max_usuarios, p.features, c.trial_expires_at, c.status
                    FROM clientes c
                    JOIN planos p ON c.plano_id = p.id
                    WHERE c.id = %s
                """, (cliente_id,))
                
                result = cur.fetchone()
                if not result:
                    return None
                
                plano_base = {
                    "id": result[0],
                    "nome": result[1],
                    "descricao": result[2],
                    "tipo": result[3],
                    "preco_base": float(result[4]) if result[4] else 0.0,
                    "max_clientes": result[5],
                    "max_usuarios": result[6],
                    "features": result[7] or {},
                    "trial_expires_at": None, # Força o valor para None, alinhando com a nova regra.
                    "status_cliente": "ativo" # Força o status para 'ativo'.
                }
                
                # Buscar desconto aplicável
                cur.execute("""
                    SELECT tipo, valor, expira_em, descricao
                    FROM descontos_cliente 
                    WHERE cliente_id = %s AND plano_id = %s AND ativo = true 
                    AND (expira_em IS NULL OR expira_em > CURRENT_TIMESTAMP)
                """, (cliente_id, result[0]))
                
                desconto_result = cur.fetchone()
                preco_final = plano_base["preco_base"]
                desconto_aplicado = None
                
                if desconto_result:
                    tipo_desconto, valor_desconto, expira_em, descricao = desconto_result
                    
                    # Converte o valor do desconto para float para garantir compatibilidade nos cálculos
                    valor_desconto_float = float(valor_desconto)

                    if tipo_desconto == 'percentual':
                        preco_final = preco_final * (1 - valor_desconto_float / 100)
                        desconto_aplicado = {
                            "tipo": "percentual",
                            "valor": valor_desconto_float,
                            "economia": plano_base["preco_base"] - preco_final,
                            "expira_em": expira_em,
                            "descricao": descricao
                        }
                    else:  # fixo
                        preco_final = max(0, preco_final - valor_desconto_float)
                        desconto_aplicado = {
                            "tipo": "fixo",
                            "valor": valor_desconto_float,
                            "economia": valor_desconto_float,
                            "expira_em": expira_em,
                            "descricao": descricao
                        }
                
                return {
                    **plano_base,
                    "preco_final": preco_final,
                    "desconto": desconto_aplicado,
                    "em_trial": False, # Força o valor para False.
                    "dias_restantes_trial": 0 # Força o valor para 0.
                }
                
    except Exception as e:
        logger.error(f"Erro ao buscar plano do cliente {cliente_id}: {e}")
        return None

def criar_desconto_cliente(cliente_id: str, plano_id: int, tipo: str, valor: float, 
                          expira_em: datetime = None, descricao: str = None):
    """Cria um desconto personalizado para um cliente - OTIMIZADO"""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    INSERT INTO descontos_cliente (cliente_id, plano_id, tipo, valor, expira_em, descricao)
                    VALUES (%s, %s, %s, %s, %s, %s)
                    ON CONFLICT (cliente_id, plano_id) 
                    DO UPDATE SET 
                        tipo = EXCLUDED.tipo,
                        valor = EXCLUDED.valor,
                        expira_em = EXCLUDED.expira_em,
                        descricao = EXCLUDED.descricao,
                        ativo = true,
                        updated_at = CURRENT_TIMESTAMP
                    RETURNING id
                """, (cliente_id, plano_id, tipo, valor, expira_em, descricao))
                
                desconto_id = cur.fetchone()[0]
                conn.commit()
                
                logger.info(f"✅ Desconto criado para cliente {cliente_id}: {tipo} {valor}")
                return desconto_id
                
    except Exception as e:
        logger.error(f"Erro ao criar desconto: {e}")
        raise

def atribuir_plano_cliente(cliente_id: str, plano_id: int, remover_trial: bool = True):
    """Atribui um plano a um cliente - OTIMIZADO"""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                update_fields = ["plano_id = %s", "updated_at = CURRENT_TIMESTAMP"]
                params = [plano_id, cliente_id]
                
                if remover_trial:
                    update_fields.append("trial_expires_at = NULL")
                    # Se estava em trial, ativar conta
                    update_fields.append("status = 'ativo'")
                
                query = f"""
                    UPDATE clientes 
                    SET {', '.join(update_fields)}
                    WHERE id = %s
                """
                
                cur.execute(query, params)
                conn.commit()
                
                logger.info(f"✅ Plano {plano_id} atribuído ao cliente {cliente_id}")
                
    except Exception as e:
        logger.error(f"Erro ao atribuir plano: {e}")
        raise

def verificar_limites_plano(cliente_id: str, recurso: str, quantidade: int = 1) -> bool:
    """Verifica se o cliente pode criar mais um recurso baseado no plano - OTIMIZADO"""
    plano = get_plano_do_cliente(cliente_id)
    if not plano:
        return False
    
    # Mapeamento de recursos para limites do plano
    limites_map = {
        'clientes': 'max_clientes',
        'usuarios': 'max_usuarios'
    }
    
    if recurso not in limites_map:
        return True  # Recurso não limitado
    
    campo_limite = limites_map[recurso]
    limite_plano = plano.get(campo_limite, 0)
    
    if limite_plano == 0:  # Ilimitado
        return True
    
    # Contar recursos existentes
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                if recurso == 'clientes':
                    cur.execute("""
                        SELECT COUNT(*) FROM clientes_loja cl
                        JOIN lojas l ON cl.loja_id = l.id
                        WHERE l.cliente_id = %s
                    """, (cliente_id,))
                elif recurso == 'usuarios':
                    cur.execute("SELECT COUNT(*) FROM users WHERE cliente_id = %s", (cliente_id,))
                
                count = cur.fetchone()[0]
                
                return (count + quantidade) <= limite_plano
                
    except Exception as e:
        logger.error(f"Erro ao verificar limites: {e}")
        return False

def verificar_e_atualizar_status_trial(cliente_id: str) -> bool:
    """Verifica se trial expirou e atualiza status se necessário - OTIMIZADO"""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                # Buscar dados do cliente
                cur.execute("""
                    SELECT trial_expires_at, status, plano_id
                    FROM clientes 
                    WHERE id = %s
                """, (cliente_id,))
                
                result = cur.fetchone()
                if not result:
                    return False
                    
                trial_expires_at, status, plano_id = result
                
                # Se trial expirou e ainda está como trial, atualizar para inativo
                if (trial_expires_at and 
                    trial_expires_at < datetime.now() and 
                    status == 'trial'):
                    
                    cur.execute("""
                        UPDATE clientes 
                        SET status = 'inativo', updated_at = CURRENT_TIMESTAMP
                        WHERE id = %s
                    """, (cliente_id,))
                    
                    # Inativar usuários também
                    cur.execute("""
                        UPDATE users 
                        SET is_active = false 
                        WHERE cliente_id = %s
                    """, (cliente_id,))
                    
                    conn.commit()
                    logger.info(f"✅ Trial expirado para cliente {cliente_id} - status atualizado para inativo")
                    return True
                
                return False
                
    except Exception as e:
        logger.error(f"Erro ao verificar status trial: {e}")
        return False

def get_estatisticas_uso_cliente(cliente_id: str) -> Dict[str, Any]:
    """Obtém estatísticas de uso do cliente para mostrar no dashboard - OTIMIZADO"""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                # Agora, ela conta apenas o número de usuários associados ao cliente (tenant).
                cur.execute("""
                    SELECT 
                        COUNT(DISTINCT u.id) as total_usuarios
                    FROM clientes c
                    LEFT JOIN users u ON c.id = u.cliente_id
                    WHERE c.id = %s
                    GROUP BY c.id
                """, (cliente_id,))
                
                result = cur.fetchone()
                estatisticas = {
                    "clientes": 0, # Mantido como 0, pois a lógica de 'clientes' do cliente não se aplica.
                    "usuarios": result[0] if result else 0
                }
                
                # Adicionar limites do plano
                plano = get_plano_do_cliente(cliente_id)
                if plano:
                    estatisticas["limites"] = {
                        "max_clientes": plano.get("max_clientes", 0),
                        "max_usuarios": plano.get("max_usuarios", 0)
                    }
                    
                    # Calcular percentuais de uso
                    limite_usuarios = estatisticas["limites"]["max_usuarios"]
                    usado_usuarios = estatisticas["usuarios"]
                    if limite_usuarios > 0:
                        estatisticas["percentual_usuarios"] = min(100, (usado_usuarios / limite_usuarios) * 100)
                    else:
                        estatisticas["percentual_usuarios"] = 0
                
                return estatisticas
                
    except Exception as e:
        logger.error(f"Erro ao obter estatísticas de uso: {e}")
        return {
            "clientes": 0,
            "usuarios": 0,
            "limites": {
                "max_clientes": 0,
                "max_usuarios": 0
            },
            "percentual_clientes": 0,
            "percentual_usuarios": 0
        }

def get_desconto_by_id(desconto_id: int) -> Optional[Dict[str, Any]]:
    """Busca um desconto específico pelo seu ID."""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT id, cliente_id, plano_id, tipo, valor, expira_em, descricao, ativo, created_at, updated_at
                    FROM descontos_cliente
                    WHERE id = %s
                """, (desconto_id,))
                
                result = cur.fetchone()
                if not result:
                    return None
                
                # Constrói um dicionário com os nomes das colunas
                columns = [desc[0] for desc in cur.description]
                return dict(zip(columns, result))
    except Exception as e:
        logger.error(f"Erro ao buscar desconto por ID {desconto_id}: {e}")
        return None

# Funções auxiliares para cache
def clear_planos_cache():
    """Limpa todos os caches de planos - útil para desenvolvimento"""
    global _planos_cache, _planos_publicos_cache, _plano_trial_cache
    _planos_cache = None
    _planos_publicos_cache = None
    _plano_trial_cache = None
    logger.info("✅ Caches de planos limpos")

def get_cache_status() -> Dict[str, Any]:
    """Retorna status dos caches para monitoramento"""
    current_time = time.time()
    
    status = {
        "planos_publicos": {
            "cached": _planos_publicos_cache is not None,
            "age_seconds": current_time - _planos_publicos_cache_time if _planos_publicos_cache else None,
            "ttl_seconds": _PLANOS_PUBLICOS_CACHE_TTL
        },
        "plano_trial": {
            "cached": _plano_trial_cache is not None,
            "age_seconds": current_time - _plano_trial_cache_time if _plano_trial_cache else None,
            "ttl_seconds": _PLANO_TRIAL_CACHE_TTL
        },
        "planos_geral": {
            "cached": _planos_cache is not None,
            "age_seconds": current_time - _planos_cache_time if _planos_cache else None,
            "ttl_seconds": _PLANOS_CACHE_TTL
        }
    }
    
    return status

def update_cliente_status(cliente_id: str, status: str, expiry_date: str = None) -> bool:
    """
    Atualiza o status e data de expiração de um cliente.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                if expiry_date:
                    cur.execute("""
                        UPDATE clientes 
                        SET status = %s, expiry_date = %s, updated_at = NOW()
                        WHERE id = %s
                    """, (status, expiry_date, cliente_id))
                else:
                    cur.execute("""
                        UPDATE clientes 
                        SET status = %s, updated_at = NOW()
                        WHERE id = %s
                    """, (status, cliente_id))
                
                conn.commit()
                return cur.rowcount > 0
    except Exception as e:
        logger.error(f"Erro ao atualizar status do cliente {cliente_id}: {e}")
        return False

def update_cliente_whatsapp_webhooks_enabled(cliente_id: str, enabled: bool) -> bool:
    """
    Atualiza a flag `whatsapp_webhooks_enabled` para um cliente (tenant).
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    UPDATE clientes
                    SET whatsapp_webhooks_enabled = %s, updated_at = NOW()
                    WHERE id = %s
                """, (enabled, cliente_id))
                conn.commit()
                return cur.rowcount > 0
    except Exception as e:
        logger.error(f"Erro ao atualizar whatsapp_webhooks_enabled para cliente {cliente_id}: {e}")
        return False

def get_user_by_cliente_id(cliente_id: str) -> list:
    """
    Obtém todos os usuários de um cliente.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT id, username, email, is_active
                    FROM users 
                    WHERE cliente_id = %s
                """, (cliente_id,))
                
                users = []
                for row in cur.fetchall():
                    users.append({
                        "id": row[0],
                        "username": row[1],
                        "email": row[2],
                        "is_active": row[3]
                    })
                return users
    except Exception as e:
        logger.error(f"Erro ao buscar usuários do cliente {cliente_id}: {e}")
        return []

def update_user_status(user_id: int, is_active: bool) -> bool:
    """
    Atualiza o status de ativação de um usuário.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    UPDATE users 
                    SET is_active = %s
                    WHERE id = %s
                """, (is_active, user_id))
                
                conn.commit()
                return cur.rowcount > 0
    except Exception as e:
        logger.error(f"Erro ao atualizar status do usuário {user_id}: {e}")
        return False

def get_all_clientes() -> List[Dict[str, Any]]:
    """Busca todos os clientes (tenants) do sistema para o painel de admin."""
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cur:
                cur.execute("""
                    SELECT 
                        c.id, 
                        c.razao_social, 
                        c.email_contato, 
                        c.status,
                        p.nome as plano_nome
                    FROM clientes c
                    LEFT JOIN planos p ON c.plano_id = p.id
                    ORDER BY c.created_at DESC;
                """)
                clientes = [dict(row) for row in cur.fetchall()]
                return clientes
    except Exception as e:
        logger.error(f"Erro ao buscar todos os clientes: {e}")
        return []

def get_cliente_details_for_admin(cliente_id: str) -> Optional[Dict[str, Any]]:
    """
    Busca todos os detalhes de um cliente para o painel de administração,
    incluindo informações do plano e usuários associados.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cur:
                cur.execute("""
                    SELECT
                        c.id, c.razao_social, c.nome_fantasia, c.documento, c.tipo,
                        c.email_contato, c.telefone, c.status, c.created_at, c.updated_at,
                        c.trial_expires_at, c.expiry_date,
                        coalesce(c.whatsapp_webhooks_enabled, c.webhooks_enabled, false) as whatsapp_webhooks_enabled,
                        p.id AS plano_id, p.nome AS plano_nome, p.descricao AS plano_descricao,
                        p.preco_mensal AS plano_preco_mensal, p.max_usuarios AS plano_max_usuarios,
                        p.features AS plano_features
                    FROM clientes c
                    LEFT JOIN planos p ON c.plano_id = p.id
                    WHERE c.id = %s
                """, (cliente_id,))
                cliente_data = cur.fetchone()

                if not cliente_data:
                    return None

                cliente_dict = dict(cliente_data)
                
                # Buscar usuários associados
                from app.database.users import get_users_by_cliente_id
                cliente_dict['usuarios'] = get_users_by_cliente_id(cliente_id)

                # Buscar descontos ativos
                # TODO: Implementar get_descontos_ativos_cliente(cliente_id)
                cliente_dict['descontos'] = [] 
                # Garantir que o campo whatsapp_webhooks_enabled existe no dicionário de retorno
                if 'whatsapp_webhooks_enabled' not in cliente_dict:
                    cliente_dict['whatsapp_webhooks_enabled'] = False
                return cliente_dict
    except Exception as e:
        logger.error(f"Erro ao buscar detalhes do cliente {cliente_id} para admin: {e}")
        return None

def update_plano(plano_id: int, **kwargs) -> bool:
    """
    Atualiza os dados de um plano de assinatura específico.
    """
    if not kwargs:
        logger.warning("Nenhum campo para atualizar no plano.")
        return False
    
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                set_clause = ", ".join([f"{key} = %s" for key in kwargs.keys()])
                values = list(kwargs.values())
                values.append(plano_id)
                
                query = f"UPDATE planos SET {set_clause}, updated_at = NOW() WHERE id = %s"
                
                cur.execute(query, values)
                conn.commit()
                return cur.rowcount > 0
    except Exception as e:
        logger.error(f"Erro ao atualizar o plano {plano_id}: {e}")
        return False

def create_plano(plano_data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Cria um novo plano de assinatura no sistema."""
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cur:
                cur.execute("""
                    INSERT INTO planos (nome, descricao, preco_mensal, max_usuarios, ativo)
                    VALUES (%(nome)s, %(descricao)s, %(preco)s, %(max_usuarios)s, %(ativo)s)
                    RETURNING *;
                """, plano_data)
                novo_plano = dict(cur.fetchone())
                conn.commit()
                return novo_plano
    except Exception as e:
        logger.error(f"Erro ao criar novo plano: {e}")
        return None

def delete_plano_by_id(plano_id: int) -> bool:
    """Exclui um plano de assinatura do sistema."""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("DELETE FROM planos WHERE id = %s", (plano_id,))
                conn.commit()
                return cur.rowcount > 0
    except Exception as e:
        logger.error(f"Erro ao excluir o plano {plano_id}: {e}")
        return False
