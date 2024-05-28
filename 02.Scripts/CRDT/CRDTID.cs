using System;
using System.Collections.Generic;

namespace Gather.CRDT
{
    public class CRDTID : IComparable
    {
        // CRDTID 값을 나타내는 정수 리스트
        public List<int> id;
        // CRDTID가 생성된 타임스탬프
        public ulong timestamp;

        // 기본 생성자
        public CRDTID()
        {
            id = new List<int>() { 0 };
            timestamp = GetTimestamp();
        }

        // 인접한 CRDTID를 기준으로 새로운 CRDTID를 생성하는 생성자
        public CRDTID(CRDTID neighborId, bool isBefore)
        {
            id = new List<int>();

            if (isBefore)
            {
                id.Add(neighborId.id[0] - 1);
            }
            else
            {
                id.Add(neighborId.id[id.Count] + 1);
            }

            timestamp = GetTimestamp();
        }

        // 두 CRDTID 사이에 새로운 CRDTID를 생성하는 생성자
        public CRDTID(CRDTID beforeId, CRDTID afterId)
        {
            id = new List<int>();

            if (beforeId.id.Count == afterId.id.Count)
            {
                for (int index = 0; index < beforeId.id.Count; index++)
                {
                    id.Add(beforeId.id[index]);
                }
                id.Add(0);
            }
            else if (beforeId.id.Count > afterId.id.Count)
            {
                for (int index = 0; index < beforeId.id.Count - 1; index++)
                {
                    id.Add(beforeId.id[index]);
                }
                id.Add(beforeId.id[beforeId.id.Count - 1] + 1);
            }
            else
            {
                for (int index = 0; index < afterId.id.Count - 1; index++)
                {
                    id.Add(afterId.id[index]);
                }
                id.Add(afterId.id[afterId.id.Count - 1] - 1);
            }

            timestamp = GetTimestamp();
        }

        // 현재 타임스탬프를 밀리초 단위로 반환하는 메서드
        ulong GetTimestamp()
        {
            TimeSpan timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return ((ulong)timeSpan.TotalMilliseconds % 100000);
        }

        // CRDTID를 문자열로 변환하는 메서드
        public override string ToString()
        {
            string result = "";
            foreach (int i in id)
            {
                result += "i";
            }
            return result + ":" + timestamp.ToString();
        }

        /// <summary>
        /// CRDTID를 서로 비교할 때 사용되는 함수 (인터페이스 구현)
        /// </summary>
        public int CompareTo(object obj)
        {
            CRDTID other = (CRDTID)obj;

            if (other == null)
            {
                throw new ApplicationException("CRDTID 객체가 아닙니다.");
            }

            int forLength = Math.Min(id.Count, other.id.Count);

            for (int index = 0; index < forLength; index++)
            {
                if (id[index] < other.id[index])
                {
                    return -1;
                }
                else if (id[index] > other.id[index])
                {
                    return 1;
                }
                else
                {
                    if (index == forLength - 1)
                    {
                        if (id.Count < other.id.Count)
                        {
                            return -1;
                        }
                        else if (id.Count > other.id.Count)
                        {
                            return 1;
                        }
                        else
                        {
                            if (timestamp > other.timestamp)
                            {
                                return -1;
                            }
                            else if (timestamp < other.timestamp)
                            {
                                return 1;
                            }
                            else
                            {
                                return 0;
                            }
                        }
                    }
                }
            }
            return 0;
        }
    }
}