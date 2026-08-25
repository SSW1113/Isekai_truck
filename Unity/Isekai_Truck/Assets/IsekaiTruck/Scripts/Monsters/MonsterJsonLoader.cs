using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IsekaiTruck.Monsters
{
    public static class MonsterJsonLoader
    {
        public static Dictionary<string, MonsterData> Load(string json)
        {
            Dictionary<string, MonsterData> monsterTypes = new Dictionary<string, MonsterData>();
            int index = 0;

            SkipWhitespace(json, ref index);
            Expect(json, ref index, '{');
            SkipWhitespace(json, ref index);

            while (index < json.Length && json[index] != '}')
            {
                string typeId = ReadString(json, ref index);

                SkipWhitespace(json, ref index);
                Expect(json, ref index, ':');
                SkipWhitespace(json, ref index);

                string typeJson = ReadObject(json, ref index);
                MonsterJsonData source = JsonUtility.FromJson<MonsterJsonData>(typeJson);

                if (source == null)
                {
                    throw new FormatException($"몬스터 데이터를 읽지 못했습니다: {typeId}");
                }

                if (!ColorUtility.TryParseHtmlString(source.color, out Color color))
                {
                    throw new FormatException($"몬스터 색상 형식이 잘못되었습니다: {typeId} / {source.color}");
                }

                MonsterData type = new MonsterData(
                    typeId,
                    source.name,
                    source.color,
                    color,
                    source.size,
                    source.speed,
                    source.fleeDistance,
                    source.exp,
                    source.soul,
                    source.spawnWeight
                );

                if (monsterTypes.ContainsKey(typeId))
                {
                    throw new FormatException($"몬스터 타입 ID가 중복되었습니다: {typeId}");
                }

                monsterTypes.Add(typeId, type);

                SkipWhitespace(json, ref index);

                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    SkipWhitespace(json, ref index);
                    continue;
                }

                break;
            }

            Expect(json, ref index, '}');
            SkipWhitespace(json, ref index);

            if (index != json.Length)
            {
                throw new FormatException($"몬스터 JSON 끝에 잘못된 데이터가 있습니다: {index}");
            }

            return monsterTypes;
        }

        private static string ReadObject(string json, ref int index)
        {
            if (index >= json.Length || json[index] != '{')
            {
                throw new FormatException($"몬스터 객체가 필요합니다: {index}");
            }

            int startIndex = index;
            int depth = 0;
            bool isInString = false;
            bool isEscaped = false;

            while (index < json.Length)
            {
                char value = json[index++];

                if (isInString)
                {
                    if (isEscaped)
                    {
                        isEscaped = false;
                    }
                    else if (value == '\\')
                    {
                        isEscaped = true;
                    }
                    else if (value == '"')
                    {
                        isInString = false;
                    }

                    continue;
                }

                if (value == '"')
                {
                    isInString = true;
                }
                else if (value == '{')
                {
                    depth++;
                }
                else if (value == '}')
                {
                    depth--;

                    if (depth == 0)
                    {
                        return json.Substring(startIndex, index - startIndex);
                    }
                }
            }

            throw new FormatException($"몬스터 객체가 닫히지 않았습니다: {startIndex}");
        }

        private static string ReadString(string json, ref int index)
        {
            Expect(json, ref index, '"');
            StringBuilder result = new StringBuilder();

            while (index < json.Length)
            {
                char value = json[index++];

                if (value == '"')
                {
                    return result.ToString();
                }

                if (value != '\\')
                {
                    result.Append(value);
                    continue;
                }

                if (index >= json.Length)
                {
                    break;
                }

                char escaped = json[index++];

                switch (escaped)
                {
                    case '"': result.Append('"'); break;
                    case '\\': result.Append('\\'); break;
                    case '/': result.Append('/'); break;
                    case 'b': result.Append('\b'); break;
                    case 'f': result.Append('\f'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    case 'u':
                        if (index + 4 > json.Length)
                        {
                            throw new FormatException($"유니코드 이스케이프가 잘못되었습니다: {index}");
                        }

                        result.Append((char)Convert.ToInt32(json.Substring(index, 4), 16));
                        index += 4;
                        break;
                    default:
                        throw new FormatException($"문자열 이스케이프가 잘못되었습니다: {escaped}");
                }
            }

            throw new FormatException("문자열이 닫히지 않았습니다.");
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }

        private static void Expect(string json, ref int index, char expected)
        {
            if (index >= json.Length || json[index] != expected)
            {
                throw new FormatException($"'{expected}' 문자가 필요합니다: {index}");
            }

            index++;
        }

        [Serializable]
        private sealed class MonsterJsonData
        {
            public string name;
            public string color;
            public float size;
            public float speed;
            public float fleeDistance;
            public int exp;
            public int soul;
            public float spawnWeight = 1f;
        }
    }
}
