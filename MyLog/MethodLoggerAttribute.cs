using MethodDecorator.Fody.Interfaces;
using System;
using System.Reflection;

namespace MyLog
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
    public class MethodLoggerAttribute : Attribute, IMethodDecorator
    {
        private MethodBase _method;
        private object[] _arguments;

        public void Init(object instance, MethodBase method, object[] args)
        {
            _method = method;
            _arguments = args;
        }

        public void OnEntry()
        {
            // 从静态桥梁获取 Logger，如果 App 还没启动完成，可能为空，需要判空
            AopLogManager.ServiceProvider?.Debug("--> Entering {MethodName} with args: {@Args}", _method.Name, (object)_arguments);
        }

        public void OnExit()
        {
            AopLogManager.ServiceProvider?.Debug("<-- Exiting {MethodName}", _method.Name);
        }

        public void OnException(Exception exception)
        {
            AopLogManager.ServiceProvider?.Error(exception, "!! Exception in {MethodName} with args: {@Args}", _method.Name, (object)_arguments);
        }
    }
}