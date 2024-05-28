using System;
using System.Collections.Generic;
using Photon.Pun;

namespace Gather.CRDT
{
    public class CRDT
    {
        // CRDT 데이터 구조: 키는 CRDTID, 값은 문자(char)
        SortedList<CRDTID, char> characters;
        // CRDT 데이터가 변경되었을 때 호출되는 이벤트
        public Action OnCRDTChanged;
        PhotonView photonView;

        // CRDT 생성자
        public CRDT(PhotonView pv)
        {
            characters = new SortedList<CRDTID, char>();
            photonView = pv;
        }

        // CRDT 데이터를 문자열로 변환하는 메서드
        public string ListToString()
        {
            string str = "";
            foreach (KeyValuePair<CRDTID, char> entry in characters)
            {
                str += entry.Value;
            }
            return str;
        }

        /// <summary>
        /// 현재 상황에 맞는 새로운 CRDTID(newID)를 생성해 줌
        /// </summary>
        public CRDTID GetInsertID(int index, char value)
        {
            CRDTID newID = null;

            if (characters.Count == 0)
            {
                newID = new CRDTID();
            }
            else if (index == 0)
            {
                newID = new CRDTID(characters.Keys[index], true);
            }
            else if (index == characters.Count)
            {
                newID = new CRDTID(characters.Keys[index - 1], false);
            }
            else
            {
                newID = new CRDTID(characters.Keys[index - 1], characters.Keys[index]);
            }

            return newID;
        }

        // 로컬에서 문자 삽입 시 호출되는 메서드
        public void LocalInsert(string newID, int value)
        {
            CRDTID crdtId = JsonUtility.FromJson<CRDTID>(newID);
            characters.Add(crdtId, (char)value);
        }

        // 원격에서 문자 삽입 시 호출되는 메서드 (RPC)
        public void RpcInsert(string newID, int value)
        {
            CRDTID crdtId = JsonUtility.FromJson<CRDTID>(newID);
            characters.Add(crdtId, (char)value);
            OnCRDTChanged?.Invoke();
        }

        // 로컬에서 문자 삭제 시 호출되는 메서드
        public void LocalDelete(int index)
        {
            characters.RemoveAt(index);
        }

        // 원격에서 문자 삭제 시 호출되는 메서드 (RPC)
        public void RpcDelete(int index)
        {
            characters.RemoveAt(index);
            if (!photonView.IsMine)
                OnCRDTChanged?.Invoke();
        }
    }
}