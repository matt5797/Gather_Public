using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// PlayerCall -> CharacterController -> Player
// ���� �� �÷��̾��� �ൿ�� �����ϴ� ������Ʈ��
namespace Gather.Interact
{
    public class PlayerCall : InteractGroup<PlayerCall>
    {
        public Transform playerSeat;

        public override void Awake()
        {
            commandList = new List<InteractCommand<PlayerCall>>();
            base.Awake();
            icon = Resources.Load<Sprite>("Icons/Interact/PlayerCall");
        }

        public override int Priority { get { return (int)InteractGroupPriority.PlayerCall; } }

        public override void SetDefaultCommands()
        {
            base.SetDefaultCommands();
            AddCommand(new CallMethod() { priority = 100 });
        }

        public override void AddCommand(InteractCommandBase command)
        {
            base.AddCommand(command);
            if (command is InteractCommand<PlayerCall>)
            {
                InteractCommand<PlayerCall> c = command as InteractCommand<PlayerCall>;
                c.target = this;
                commandList.Add(c);
            }
        }
    }

    #region Commands
    [System.Serializable]
    public class CallMethod : InteractCommand<PlayerCall>
    {
        public CallMethod()
        {
            authority = 0;
            priority = 0;
            isActivate = true;
            password = "";
            isPassword = false;
            title = "�÷��̾� ����: ";
        }
        
        public override void Execute()
        {
            base.Execute();
            Debug.Log("Call Method");
            // add command
            Gather.Controller.CharacterController.Instance.ChairInteract(target.playerSeat);
        }
    }
    #endregion
}
