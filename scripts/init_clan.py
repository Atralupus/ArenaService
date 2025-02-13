import os
import csv
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
    for component in dotnet_string.split(";"):
        if "=" in component:
            key, value = component.split("=", 1)
            key = mapping.get(key, key).lower()
            converted.append(f"{key}={value}")
    return " ".join(converted)

CONVERTED_CONNECTION_STRING = convert_connection_string(CONNECTION_STRING)

def load_avatars_data(network):
    """ avatars/{network}.json 파일을 로드하여 avatar_address → 정보 매핑 """
    avatars_file = f"avatars/{network}.json"
    try:
        with open(avatars_file, "r", encoding="utf-8") as file:
            data = json.load(file)
            return {entry["Address"][2:].lower(): entry for entry in data}
    except FileNotFoundError:
        print(f"⚠️ Avatars JSON 파일을 찾을 수 없습니다: {avatars_file}")
        return {}
    except json.JSONDecodeError as e:
        print(f"❌ Avatars JSON 파일 파싱 오류: {e}")
        return {}

def get_user_data(avatar_address, agent_address, avatars_map):
    """ 유저가 DB에 없으면 avatars JSON 데이터에서 가져오기 """
    avatar_info = avatars_map.get(avatar_address)
    if avatar_info:
        return {
            "agent_address": agent_address.lower(),
            "avatar_address": avatar_address.lower(),
            "name_with_hash": f"{avatar_info['Name']} <size=80%><color=#A68F7E>#{avatar_address[-4:]}</color></size>",
            "portrait_id": avatar_info["ArmorId"],
            "cp": avatar_info["Cp"],
            "level": avatar_info["AvatarLevel"]
        }
    return None

def get_clan_id(clan_name, conn):
    """ 클랜 ID 조회 및 없으면 생성 """
    with conn.cursor() as cursor:
        cursor.execute("SELECT id FROM clans WHERE name = %s", (clan_name,))
        result = cursor.fetchone()
        if result:
            return result[0]

        cursor.execute("""
            INSERT INTO clans (name, image_url, created_at, updated_at)
            VALUES (%s, %s, now(), now())
            RETURNING id
        """, (clan_name, "https://example.com/default_clan_image.png"))
        conn.commit()
        return cursor.fetchone()[0]

def user_exists(avatar_address, conn):
    """ 유저 존재 여부 확인 """
    with conn.cursor() as cursor:
        cursor.execute("SELECT COUNT(*) FROM users WHERE avatar_address = %s", (avatar_address,))
        return cursor.fetchone()[0] > 0

def add_user(user_data, conn):
    """ 유저 추가 (없을 경우만) """
    if user_data:
        with conn.cursor() as cursor:
            cursor.execute("""
                INSERT INTO users (avatar_address, agent_address, name_with_hash, portrait_id, cp, level, created_at, updated_at)
                VALUES (%s, %s, %s, %s, %s, %s, now(), now())
            """, (user_data["avatar_address"], user_data["agent_address"], user_data["name_with_hash"],
                  user_data["portrait_id"], user_data["cp"], user_data["level"]))
        conn.commit()

def assign_user_to_clan(avatar_address, clan_id, conn):
    """ 유저를 클랜에 배정 (이미 배정된 경우 유지) """
    with conn.cursor() as cursor:
        cursor.execute("SELECT clan_id FROM users WHERE avatar_address = %s", (avatar_address,))
        result = cursor.fetchone()
        if result and result[0]:  # 이미 클랜이 할당된 경우
            return

        cursor.execute("""
            UPDATE users SET clan_id = %s, updated_at = now()
            WHERE avatar_address = %s
        """, (clan_id, avatar_address))
        conn.commit()

def process_csv(network):
    """ CSV 파일을 읽고 데이터베이스에 저장 """
    csv_file_path = f"clan/{network}.csv"
    avatars_map = load_avatars_data(network)

    try:
        with psycopg2.connect(CONVERTED_CONNECTION_STRING) as conn:
            with open(csv_file_path, newline="", encoding="utf-8") as csvfile:
                reader = csv.reader(csvfile)
                header = next(reader)  # 첫 번째 행 (헤더)

                avatar_indexes = [i for i, col in enumerate(header) if "Avatar Addreses" in col]
                agent_indexes = [i for i, col in enumerate(header) if "Agent Address" in col]

                for row in reader:
                    clan_name = row[0].strip()
                    clan_id = get_clan_id(clan_name, conn)

                    for idx in range(len(avatar_indexes)):
                        avatar_address = row[avatar_indexes[idx]].strip().lower()[2:]  # "0x" 제거
                        agent_address = row[agent_indexes[idx]].strip().lower()[2:]

                        if not avatar_address:
                            continue  # 데이터가 없으면 건너뛰기

                        # 유저 존재 확인 및 추가
                        if not user_exists(avatar_address, conn):
                            user_data = get_user_data(avatar_address, agent_address, avatars_map)
                            if user_data:
                                add_user(user_data, conn)
                            else:
                                print(f"⚠️ avatars JSON에서도 데이터를 찾을 수 없음: {avatar_address}")

                        assign_user_to_clan(avatar_address, clan_id, conn)

                print("✅ CSV 데이터 처리 완료")

    except Exception as e:
        print(f"❌ 데이터 처리 중 오류 발생: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="클랜 CSV 데이터를 처리하는 스크립트")
    parser.add_argument("network", type=str, choices=["thor", "heimdall", "odin"], help="체인 선택 (thor, heimdall, odin)")
    args = parser.parse_args()

    process_csv(args.network)
