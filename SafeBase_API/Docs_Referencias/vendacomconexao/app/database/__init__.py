# Expõe as funções dos submódulos para que possam ser importadas diretamente de 'app.database'
# app/database/__init__.py
from .core import (
    get_db_connection,
    get_total_users_count,
    get_clients_summary_metrics,
    get_pending_webhooks_count
)
from .planos import (
    create_cliente,
    get_planos_publicos,
    get_plano_trial,
    get_plano_inicial_padrao,
    get_plano_by_id,
    get_cliente_by_id,
    get_plano_do_cliente,
    get_cliente_by_email,
    criar_desconto_cliente,
    atribuir_plano_cliente,
    get_estatisticas_uso_cliente,
    get_desconto_by_id,
    update_cliente_status,
    get_user_by_cliente_id,
    update_user_status, # type: ignore
    get_all_clientes,
    update_plano,
    get_cliente_details_for_admin,
    get_all_planos_for_admin,
    create_plano,
    delete_plano_by_id
)
from .prompts import (
    get_prompt_versoes,
    create_prompt_versao,
    update_prompt_versao_status,
    get_active_prompt_versoes_for_testing,
    update_prompt_versao
)
from .rbac import (
    get_user_profile,
    get_profile_by_name,
    get_profile_permissions,
    get_api_key_profile,
    create_api_key,
    deactivate_api_key,
    get_all_profiles,
    get_all_modules,
    assign_user_profile,
    get_active_api_keys
)
from .rbac import (
    get_profile_by_id, update_profile_name, clear_profile_permissions, 
    add_permission_to_profile, get_module_by_name
)
from .users import (
    get_user_by_username,
    get_user_by_login_identifier,
    create_user,
    get_user_by_email,
    update_user_password,
    update_user,
    get_user_details_by_id,
    get_user_by_id,
    get_all_users,
    get_users_by_cliente_id
)
from .history import (
    save_simulation,
    get_user_simulations,
    user_has_simulations,
    get_example_scripts,
    create_example_simulation,
    get_user_simulations_secure,
    validate_simulation_ownership,
    get_user_sessions 
)
from .webhooks_db import (
    log_webhook_event,
    get_webhook_logs,
    get_webhook_event_by_id,
)
from .logs_db import (
    get_api_logs,
    # log_api_request # Removido, pois o logging agora é assíncrono via handler
)

from .config_db import (
    get_all_system_configs,
    update_system_config
) 

from .email_templates_db import (
    get_email_template_by_chave,
    get_all_email_templates,
    get_email_template_by_id,
    create_email_template,
    update_email_template,
    delete_email_template
)
