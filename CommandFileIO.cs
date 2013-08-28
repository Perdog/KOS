﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;

namespace kOS
{
    [CommandAttribute(@"^EDIT (.+?)$")]
    public class CommandEditFile : Command
    {
        public CommandEditFile(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String fileName = RegexMatch.Groups[1].Value;

            if (ParentContext is ImmediateMode)
            {
                ParentContext.Push(new InterpreterEdit(fileName, ParentContext));
            }
            else
            {
                throw new kOSException("Edit can only be used when in immediate mode.");
            }
        }
    }
    
    [CommandAttribute(@"^RUN (.+?)$")]
    public class CommandRunFile : Command
    {
        public CommandRunFile(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }
          
        public override void Evaluate()
        {
            String fileName = RegexMatch.Groups[1].Value;
            File file = SelectedVolume.GetByName(fileName);

            if (file != null)
            {
                ContextRunProgram runContext = new ContextRunProgram(this);
                Push(runContext);

                if (file.Count > 0)
                {
                    runContext.Run(file);
                    State = ExecutionState.WAIT;
                }
                else
                {
                    State = ExecutionState.DONE;
                }
            }
            else
            {
                throw new kOSException("File not found '" + fileName + "'.");
            }
        }

        public override bool Type(char c)
        {
            if (State == ExecutionState.WAIT)
            {
                return true;
            }
            else
            {
                return base.Type(c);
            }
        }

        public override bool SpecialKey(kOSKeys key)
        {
            if (key == kOSKeys.BREAK)
            {
                StdOut("Program aborted.");
                State = ExecutionState.DONE;

                // Bypass child contexts
                return true;
            }

            return base.SpecialKey(key);
        }

        public override void Update(float time)
        {
            try
            {
                base.Update(time);
            }
            catch (kOSException e)
            {
                StdOut("Error: " + e.Message);
                StdOut("Program aborted.");
                State = ExecutionState.DONE;
            }

            if (ChildContext == null)
            {
                State = ExecutionState.DONE;
            }
            else if (ChildContext.State == ExecutionState.DONE)
            {
                StdOut("Program ended.");
                State = ExecutionState.DONE;
            }
        }
    }
    
    [CommandAttribute(@"^SWITCH TO (.+?)$")]
    public class CommandSwitch : Command
    {
        public CommandSwitch(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String targetVolume = RegexMatch.Groups[1].Value.Trim();
            int volID;

            if (int.TryParse(targetVolume, out volID))
            {
                if (!ParentContext.SwitchToVolume(volID))
                {
                    throw new kOSException("Volume " + volID + " not found");
                }
            }
            else
            {
                if (!ParentContext.SwitchToVolume(targetVolume))
                {
                    throw new kOSException("Volume '" + targetVolume + "' not found");
                }
            }

            State = ExecutionState.DONE;
        }
    }
    
    [CommandAttribute(@"^RENAME( VOLUME| FILE)? (.+?) TO (.+?)$")]
    public class CommandRename : Command
    {
        public CommandRename(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String operation = RegexMatch.Groups[1].Value.Trim();
            String identifier = RegexMatch.Groups[2].Value.Trim();
            String newName = RegexMatch.Groups[3].Value.Trim();

            if (operation.ToUpper() == "VOLUME")
            {
                Volume targetVolume = GetVolume(identifier); // Will throw if not found

                int intTry;
                if (int.TryParse(newName.Substring(0, 1), out intTry)) throw new kOSException("Volume name cannot start with numeral");

                if (targetVolume.Renameable) targetVolume.Name = newName;
                else throw new kOSException("Volume cannot be renamed");

                State = ExecutionState.DONE;
                return;
            }
            else if (operation.ToUpper() == "FILE" || String.IsNullOrEmpty(operation))
            {
                File f = SelectedVolume.GetByName(identifier);
                if (f == null) throw new kOSException("File '" + identifier + "' not found");

                int intTry;
                if (int.TryParse(newName.Substring(0, 1), out intTry)) throw new kOSException("Filename cannot start with numeral");

                f.Filename = newName;
                State = ExecutionState.DONE;
                return;
            }

            throw new kOSException("Unrecognized renamable object type '" + operation + "'");
        }
    }

    [CommandAttribute(@"^RELOAD (ALL|VOLUME) (.+?)$")]
    public class CommandReload : Command
    {
        public CommandReload(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String reloadType = RegexMatch.Groups[1].Value.Trim();
            String identifier = RegexMatch.Groups[2].Value.Trim();

            switch(reloadType)
            {
                case "ALL":
                    String[] allDisks = Directory.GetDirectories(HighLogic.fetch.GameSaveFolder);
                    // We clear all volumes currently being tracked
                    Cpu.Volumes.Clear();
                    foreach (String disk in allDisks)
                    {
                        // And replace them one by one
                        Cpu.Volumes.Add(HarddiskLoader.LoadHarddisk(disk));
                    }
                    break;

                case "VOLUME":
                    // Try to get the volume
                    Volume vol = GetVolume(identifier);
                    // If found, reload and replace
                    vol.Files = HarddiskLoader.LoadFiles(identifier);
                    break;
            }

            State = ExecutionState.DONE;
        }
    }
    
    [CommandAttribute(@"^COPY (.+?) (TO|FROM)( VOLUME)? (.+?)$")]
    public class CommandCopy : Command
    {
        public CommandCopy(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String targetFile = RegexMatch.Groups[1].Value.Trim();
            String volumeName = RegexMatch.Groups[4].Value.Trim();
            String operation = RegexMatch.Groups[2].Value.Trim().ToUpper();

            Volume targetVolume = GetVolume(volumeName); // Will throw if not found

            File file = null;

            switch (operation)
            {
                case "FROM":
                    file = targetVolume.GetByName(targetFile);
                    if (file == null) throw new kOSException("File '" + targetFile + "' not found");
                    SelectedVolume.SaveFile(new File(file));
                    break;

                case "TO":
                    file = SelectedVolume.GetByName(targetFile);
                    if (file == null) throw new kOSException("File '" + targetFile + "' not found");
                    targetVolume.SaveFile(new File(file));
                    break;
            }

            State = ExecutionState.DONE;
        }
    }
    
    [CommandAttribute(@"^LIST( VOLUMES| FILES)?$")]
    public class CommandList : Command
    {
        public CommandList(Match regexMatch, ExecutionContext context) : base(regexMatch,  context) { }

        public override void Evaluate()
        {
            String listType = RegexMatch.Groups[1].Value.Trim().ToUpper();

            if (listType == "FILES" || String.IsNullOrEmpty(listType))
            {
                StdOut("");
                StdOut("Filename                      Size");
                StdOut("-------------------------------------");

                foreach (File File in SelectedVolume.Files)
                {
                    StdOut(File.Filename.PadRight(30, ' ') + File.GetSize().ToString());
                }

                StdOut("");

                State = ExecutionState.DONE;
                return;
            }
            else if (listType == "VOLUMES")
            {
                StdOut("");
                StdOut("ID    Name                    Size");
                StdOut("-------------------------------------");

                int i = 0;

                foreach (Volume volume in Volumes)
                {
                    String id = i.ToString();
                    if (volume == SelectedVolume) id = "*" + id;

                    String line = id.PadLeft(2).PadRight(6, ' ');
                    line += volume.Name.PadRight(24, ' ');

                    String size = volume.Capacity > -1 ? volume.Capacity.ToString() : "Inf";
                    line += size;

                    StdOut(line);

                    i++;
                }

                StdOut("");

                State = ExecutionState.DONE;
                return;
            }

            throw new kOSException("List type '" + listType + "' not recognized.");
        }
    }
}