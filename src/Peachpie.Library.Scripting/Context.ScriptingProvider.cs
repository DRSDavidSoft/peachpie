﻿using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.Scripting
{
    [Export(typeof(Context.IScriptingProvider))]
    public class ScriptingProvider : Context.IScriptingProvider
    {
        readonly Dictionary<string, List<Script>> _scripts = new Dictionary<string, List<Script>>();
        readonly PhpCompilationFactory _builder = new PhpCompilationFactory();

        sealed class ScriptingContext
        {
            public List<Script> Submissions { get; } = new List<Script>();

            public Script LastSubmission
            {
                get
                {
                    return (Submissions.Count == 0) ? null : Submissions[Submissions.Count - 1];
                }
            }
        }

        List<Script> EnsureCache(string code)
        {
            if (!_scripts.TryGetValue(code, out List<Script> candidates))
            {
                _scripts[code] = candidates = new List<Script>();
            }
            return candidates;
        }

        Script CacheLookup(Context.ScriptOptions options, string code, ScriptingContext scriptingCtx)
        {
            if (_scripts.TryGetValue(code, out List<Script> candidates))
            {
                foreach (var c in candidates)
                {
                    if (c.DependingSubmission == null || scriptingCtx.Submissions.Contains(c.DependingSubmission))
                    {
                        return c;
                    }
                }
            }

            return null;
        }

        Context.IScript Context.IScriptingProvider.CreateScript(Context.ScriptOptions options, string code)
        {
            var scriptingCtx = options.Context.GetStatic<ScriptingContext>();

            var script = CacheLookup(options, code, scriptingCtx);
            if (script == null)
            {
                // TODO: rwlock cache[code]
                script = Script.Create(options, code, _builder, scriptingCtx.LastSubmission);
                EnsureCache(code).Add(script);
            }

            Debug.Assert(script != null);

            //
            scriptingCtx.Submissions.Add(script);

            //
            return script;
        }
    }
}
