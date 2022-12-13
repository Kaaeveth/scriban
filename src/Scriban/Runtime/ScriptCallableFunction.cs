// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using Scriban.Syntax;

namespace Scriban.Runtime
{
    public class ScriptCallableFunction : IScriptCustomFunction
    {
        private MethodInfo _methodInfo;
        private object _target;

        public ScriptCallableFunction(MethodInfo methodInfo, object target)
        {
            _methodInfo = methodInfo;
            _target = target;
            ReturnType = _methodInfo.ReturnType;
            VarParamKind = ScriptVarParamKind.Direct;
            ParameterCount = _methodInfo.GetParameters().Length;
        }

        public int RequiredParameterCount { get; }
        public int ParameterCount { get; }
        public ScriptVarParamKind VarParamKind { get; }
        public Type ReturnType { get; }
        public ScriptParameterInfo GetParameterInfo(int index)
        {
            var param = _methodInfo.GetParameters()[index];
            return new ScriptParameterInfo(param.ParameterType, param.Name, param.DefaultValue);
        }

        public object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
        {
            var res = _methodInfo.Invoke(_target, arguments.ToArray()) ?? string.Empty;
            try
            {
                dynamic resTask = res;
                resTask.Wait();
                return resTask.Result;
            } catch(RuntimeBinderException) {} // Wenn es keine asynchrone Operation / Task war, dann einfach ignorieren
            return res;
        }

        public ValueTask<object> InvokeAsync(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
        {
            var res = Invoke(context, callerContext, arguments, blockStatement);
            return new ValueTask<object>(res);
        }
    }
}