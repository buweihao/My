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
            // 使用配置的委托执行日志记录
            AopLogManager.Options.OnEntry?.Invoke(AopLogManager.ServiceProvider, _method, _arguments);
        }

        public void OnExit()
        {
            AopLogManager.Options.OnExit?.Invoke(AopLogManager.ServiceProvider, _method);
        }

        public void OnException(Exception exception)
        {
            AopLogManager.Options.OnException?.Invoke(AopLogManager.ServiceProvider, _method, _arguments, exception);
        }
    }
}