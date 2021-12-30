using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JasonBright.StateMachine
{
    public interface IStateController
    {
        void OnEntered(EnterDataBase data);
        ExitDataBase OnExited();
    }

    public class EnterDataBase
    {
    }

    public class ExitDataBase
    {
    
    }
}