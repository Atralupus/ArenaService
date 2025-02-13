import os
import json
import argparse
import psycopg2
from psycopg2 import sql
from dotenv import load_dotenv

load_dotenv()

# 데이터베이스 연결 정보 설정
CONNECTION_STRING = os.getenv("DB_CONNECTION_STRING", "Host=localhost;Database=arena;Username=yourusername;Password=yourpassword")

def convert_connection_string(dotnet_string):
    mapping = {
        "Host": "host",
        "Database": "dbname",
        "Username": "user",
        "Password": "password",
    }
    components = dotnet_string.split(";")
    converted = []
    for component in components:
        if "=" in component:
            key, value = component.split("=", 1)
            key = mapping.get(key, key).lower()
            converted.append(f"{key}={value}")
    return " ".join(converted)

CONVERTED_CONNECTION_STRING = convert_connection_string(CONNECTION_STRING)

def load_participants_from_json(file_path):
    """ JSON 파일에서 참가자 데이터를 로드 """
    try:
        with open(file_path, "r", encoding="utf-8") as file:
            data = json.load(file)
            return data.get("data", {}).get("stateQuery", {}).get("arenaParticipants", [])
    except FileNotFoundError:
        print(f"⚠️ JSON 파일을 찾을 수 없습니다: {file_path}")
        return []
    except json.JSONDecodeError as e:
        print(f"❌ JSON 파일 파싱 오류: {e}")
        return []

def load_agent_addresses(avatars_file_path):
    """ avatars 폴더의 JSON 파일에서 avatar_address와 agent_address를 매핑 """
    try:
        with open(avatars_file_path, "r", encoding="utf-8") as file:
            data = json.load(file)
            return {entry["Address"][2:].lower(): entry["AgentAddress"][2:].lower() for entry in data}
    except FileNotFoundError:
        print(f"⚠️ Avatars JSON 파일을 찾을 수 없습니다: {avatars_file_path}")
        return {}
    except json.JSONDecodeError as e:
        print(f"❌ Avatars JSON 파일 파싱 오류: {e}")
        return {}

def insert_users(participants, agent_address_map):
    """ DB에 유저 정보 삽입 """
    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with conn.cursor() as cursor:
                for participant in participants:
                    avatar_address = participant["avatarAddr"][2:].lower()  # "0x" 제거
                    agent_address = agent_address_map.get(avatar_address, None)

                    if agent_address is None:
                        print(f"⚠️ AgentAddress를 찾을 수 없습니다: {avatar_address}")
                        continue
                    print(agent_address, avatar_address)

                    cursor.execute(
                        sql.SQL("""
                            INSERT INTO users (agent_address, avatar_address, name_with_hash, portrait_id, cp, level, created_at, updated_at)
                            VALUES (%s, %s, %s, %s, %s, %s, now(), now())
                            ON CONFLICT (avatar_address) DO NOTHING
                        """),
                        (agent_address.lower(), avatar_address.lower(), participant["nameWithHash"], int(participant["portraitId"]), int(participant["cp"]), int(participant["level"]))
                    )
                    conn.commit()
                print(f"✅ {len(participants)}명의 유저 삽입 완료")
    except Exception as e:
        print(f"❌ 유저 삽입 중 오류 발생: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="유저 데이터를 삽입하는 스크립트")
    parser.add_argument("chain", type=str, choices=["thor", "heimdall", "odin"], help="체인 선택 (thor, heimdall, odin)")
    parser.add_argument("--data-folder", type=str, default="./arena-data", help="유저 데이터가 위치한 폴더 경로")
    parser.add_argument("--avatars-folder", type=str, default="./avatars", help="avatars 폴더 경로")
    args = parser.parse_args()

    # JSON 파일 경로 설정
    participants_file_path = os.path.join(args.data_folder, f"{args.chain}.json")
    avatars_file_path = os.path.join(args.avatars_folder, f"{args.chain}.json")

    # 데이터 로드
    participants = load_participants_from_json(participants_file_path)
    agent_address_map = load_agent_addresses(avatars_file_path)

    if not participants:
        print("⚠️ 삽입할 유저 데이터가 없습니다.")
    else:
        insert_users(participants, agent_address_map)
