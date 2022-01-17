using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoCharacterController
{
    /// <summary>
    /// The key context bound to an action
    /// </summary>
    public enum KeyContext
    {
        Pressed,
        Released
    }

    /// <summary>
    /// A wrapper used to bind an axis with an Action.
    /// </summary>
    public struct InputAxis
    {
        public string name;
        public Action<float> method;

        public InputAxis(string name, Action<float> method)
        {
            this.name = name;
            this.method = method;
        }
    }

    /// <summary>
    /// A wrapper used to bind 2 axes with an Action
    /// </summary>
    public struct InputAxis2D
    {
        public string axisX;
        public string axisY;
        public Action<float, float> method;

        public InputAxis2D(string axisX, string axisY, Action<float, float> method)
        {
            this.axisX = axisX;
            this.axisY = axisY;
            this.method = method;
        }
    }

    /// <summary>
    /// A wrapper used to bind a key with an Action.
    /// </summary>
    public struct InputAction
    {
        public string name;
        public Action method;
        public KeyContext context;

        public InputAction(string name, Action method, KeyContext context)
        {
            this.name = name;
            this.method = method;
            this.context = context;
        }
    }

    public class ProtoInputManager : MonoBehaviour
    {
        private List<InputAxis> _boundAxes;
        private List<InputAxis2D> _boundAxes2D;
        private List<InputAction> _boundActions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]

        void Awake()
        {
            _boundActions = new List<InputAction>();
            _boundAxes = new List<InputAxis>();
            _boundAxes2D = new List<InputAxis2D>();
        }

        void OnEnable()
        {
            UnbindAll();
        }

        void OnDisable()
        {
            UnbindAll();
        }
        public void UnbindAll()
        {
            _boundActions.Clear();
            _boundAxes.Clear();
            _boundAxes2D.Clear();
        }

        void Update()
        {
            for (int i = 0; i < _boundAxes.Count; i++)
            {
                float value = Input.GetAxisRaw(_boundAxes[i].name);
                _boundAxes[i].method?.Invoke(value);
            }

            for (int i = 0; i < _boundAxes2D.Count; i++)
            {
                float valueX = Input.GetAxisRaw(_boundAxes2D[i].axisX);
                float valueY = Input.GetAxisRaw(_boundAxes2D[i].axisY);
                _boundAxes2D[i].method?.Invoke(valueX, valueY);
            }

            for (int i = 0; i < _boundActions.Count; i++)
            {
                if (Input.GetButtonDown(_boundActions[i].name) 
                    && KeyContext.Pressed == _boundActions[i].context)
                {
                    _boundActions[i].method?.Invoke();
                }
                else if (Input.GetButtonUp(_boundActions[i].name)
                    && KeyContext.Released == _boundActions[i].context)
                {
                    _boundActions[i].method?.Invoke();
                }
            }
        }

        public void BindAxis(string axisName, Action<float> method)
        {
            _boundAxes.Add(new InputAxis(axisName, method));
        }

        public void BindAxis2D(string xAxisName, string yAxisName, Action<float, float> method)
        {
            _boundAxes2D.Add(new InputAxis2D(xAxisName, yAxisName, method));
        }

        public void BindAction(string actionName, Action method, KeyContext context)
        {
            _boundActions.Add(new InputAction(actionName, method, context));
        }
    }
}

