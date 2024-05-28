using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Photon.Pun;

namespace Gather.CRDT
{
    public class CRDT_Sample : MonoBehaviourPun
    {
        TMPro.TMP_InputField inputField;
        string text;
        CRDT crdt;

        void Start()
        {
            inputField = GetComponent<TMP_InputField>();
            text = inputField.text;

            crdt = new CRDT(photonView);
            crdt.OnCRDTChanged = OnCRDTChanged;

            this.transform.parent = GameObject.Find("CanvasNote").transform;
            this.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                crdt = new CRDT(photonView);
                text = inputField.text;
            }
        }

        /// <summary>
        /// CRDT에 데이터가 변경되었을 때 호출되는 메서드
        /// </summary>
        public void OnCRDTChanged()
        {
            inputField.text = crdt.ListToString();
            text = inputField.text;
        }

        Queue<int> changeQueue = new Queue<int>();

        // 입력 필드의 값이 변경되었을 때 호출되는 메서드
        public void OnValueChanged(string value)
        {
            int timestamp = (int)((ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds % 100000);

            bool isDeleted = false;

            // 변경된 문자열과 기존 문자열을 비교하여 변경 사항을 파악
            for (int i = 0; i < value.Length; i++)
            {
                if (text.Length <= i)
                {
                    // 새로운 문자가 추가된 경우
                    Insert(i, value[i], timestamp);
                    break;
                }
                else if (text[i] != value[i])
                {
                    if (text.Length > value.Length && text[i + 1] == value[i])
                    {
                        // 문자가 삭제된 경우
                        Delete(i, timestamp);
                        isDeleted = true;
                        break;
                    }
                    else
                    {
                        // 문자가 변경된 경우
                        Insert(i, value[i], timestamp);
                        break;
                    }
                }
            }

            if (!isDeleted && text.Length > value.Length)
            {
                // 마지막 문자가 삭제된 경우
                Delete(text.Length - 1, timestamp);
            }

            text = value;
        }

        // 문자 삽입 메서드
        public void Insert(int i, char c, int timestamp)
        {
            photonView.RPC(nameof(RpcInsert), RpcTarget.OthersBuffered, JsonUtility.ToJson(crdt.GetInsertID(i, c)), (int)c, timestamp);
        }

        // 문자 삭제 메서드
        public void Delete(int index, int timestamp)
        {
            photonView.RPC(nameof(RpcDelete), RpcTarget.OthersBuffered, index, timestamp);
        }

        // 원격 클라이언트에서 문자 삽입 시 호출되는 RPC 메서드
        [PunRPC]
        public void RpcInsert(string newID, int value, int timestamp)
        {
            changeQueue.Enqueue(timestamp);
            crdt.RpcInsert(newID, value);
        }

        // 원격 클라이언트에서 문자 삭제 시 호출되는 RPC 메서드
        [PunRPC]
        public void RpcDelete(int index, int timestamp)
        {
            changeQueue.Enqueue(timestamp);
            crdt.RpcDelete(index);
        }
    }
}