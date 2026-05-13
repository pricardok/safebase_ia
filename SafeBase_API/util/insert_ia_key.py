import argparse
import hashlib
import json
import os
import sys

CURRENT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(CURRENT_DIR)
if PROJECT_ROOT not in sys.path:
    sys.path.insert(0, PROJECT_ROOT)

from app.db.models import ChaveIA, ProvedorIA
from app.db.session import SessionLocal
from app.services.crypto_manager import crypto_manager


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Insert encrypted IA key into database")
    parser.add_argument("--provider", help="Provider name (ex: openai)")
    parser.add_argument("--provider-id", type=int, help="Provider id")
    parser.add_argument("--api-key", required=True, help="Raw API key to encrypt")
    parser.add_argument("--descricao", default="chave principal", help="Description for the key")
    parser.add_argument("--ativa", action="store_true", help="Mark key as active")
    parser.add_argument("--metadados", default="{}", help="JSON metadata string")
    parser.add_argument(
        "--meta",
        action="append",
        default=[],
        help="Metadata key=value (can be repeated)",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if not args.provider and not args.provider_id:
        print("Error: --provider or --provider-id is required", file=sys.stderr)
        return 1

    metadata = {}
    if args.metadados and args.metadados.strip() != "{}":
        try:
            metadata = json.loads(args.metadados)
        except json.JSONDecodeError:
            if not args.meta:
                print("Error: --metadados must be valid JSON", file=sys.stderr)
                print("Hint: use --meta origem=manual (repeat for multiple entries)", file=sys.stderr)
                return 1

    if args.meta:
        for entry in args.meta:
            if "=" not in entry:
                print("Error: --meta must be in key=value format", file=sys.stderr)
                return 1
            key, value = entry.split("=", 1)
            metadata[key] = value

    db = SessionLocal()
    try:
        query = db.query(ProvedorIA)
        provider = None
        if args.provider_id:
            provider = query.filter(ProvedorIA.id == args.provider_id).first()
        else:
            provider = query.filter(ProvedorIA.nome == args.provider).first()

        if not provider:
            print("Error: provider not found", file=sys.stderr)
            return 1

        hash_chave = hashlib.sha256(args.api_key.encode("utf-8")).hexdigest()
        encrypted_key = crypto_manager.encrypt_key(args.api_key)

        key_record = ChaveIA(
            provedor_id=provider.id,
            hash_chave=hash_chave,
            chave_criptografada=encrypted_key,
            descricao=args.descricao,
            ativo=True if args.ativa else True,
            metadados=json.dumps(metadata, ensure_ascii=False),
        )
        db.add(key_record)
        db.commit()
        db.refresh(key_record)

        print("Inserted key:")
        print(f"  id: {key_record.id}")
        print(f"  provider: {provider.nome}")
        print(f"  hash: {hash_chave}")
        return 0
    except Exception as exc:
        db.rollback()
        print(f"Error: {exc}", file=sys.stderr)
        return 1
    finally:
        db.close()


if __name__ == "__main__":
    raise SystemExit(main())
