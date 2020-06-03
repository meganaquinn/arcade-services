// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Darc.Models;
using Microsoft.DotNet.DarcLib.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Work.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.DotNet.Darc.Helpers
{
    public class UxManager
    {
        private readonly string _editorPath;
        private readonly ILogger _logger;
        private bool _popUpClosed = false;

        public UxManager(string gitLocation, ILogger logger)
        {
            _editorPath = LocalHelpers.GetEditorPath(gitLocation, logger);
            _logger = logger;
        }

        /// <summary>
        ///     Rather than popping up the window, read the result of the popup from
        ///     stdin and process the contents.  This is primarily used for testing purposes.
        /// </summary>
        /// <param name="popUp">Popup to run</param>
        /// <returns>Success or error code</returns>
        public int ReadFromStdIn(EditorPopUp popUp)
        {
            int result = Constants.ErrorCode;

            try
            {
                // File to write from stdin to, which will be processed by the popup closing handler
                string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), popUp.Path);
                string dirPath = Path.GetDirectoryName(path);

                Directory.CreateDirectory(dirPath);
                using (System.IO.StreamWriter streamWriter = new StreamWriter(path))
                {
                    string line;
                    while ((line = Console.ReadLine()) != null)
                    {
                        streamWriter.WriteLineAsync(line);
                    }
                }

                // Now run the closed event and process the contents
                IList<Line> contents = popUp.OnClose(path);
                result = popUp.ProcessContents(contents);
                Directory.Delete(dirPath, true);
                if (result != Constants.SuccessCode)
                {
                    _logger.LogError("Inputs were invalid.");
                }

                return result;
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "There was an exception processing YAML input from stdin.");
                result = Constants.ErrorCode;
            }

            return result;
        }

        /// <summary>
        ///     Pop up the editor and allow the user to edit the contents.
        /// </summary>
        /// <param name="popUp">Popup to run</param>
        /// <returns>Success or error code</returns>
        public int PopUp(EditorPopUp popUp)
        {
            if (string.IsNullOrEmpty(_editorPath))
            {
                _logger.LogError("Failed to define an editor for the pop ups...");
                return Constants.ErrorCode;
            }

            int result = Constants.ErrorCode;
            int tries = Constants.MaxPopupTries;

            ParsedCommand parsedCommand = GetParsedCommand(_editorPath);

            try
            {
                string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), popUp.Path);
                string dirPath = Path.GetDirectoryName(path);

                Directory.CreateDirectory(dirPath);
                File.WriteAllLines(path, popUp.Contents.Select(l => l.Text));

                while (tries-- > 0 && result != Constants.SuccessCode)
                {
                    using (Process process = new Process())
                    {
                        _popUpClosed = false;
                        process.EnableRaisingEvents = true;
                        process.Exited += (sender, e) =>
                        {
                            IList<Line> contents = popUp.OnClose(path);

                            result = popUp.ProcessContents(contents);

                            // If succeeded, delete the temp file, otherwise keep it around
                            // for another popup iteration.
                            if (result == Constants.SuccessCode)
                            {
                                Directory.Delete(dirPath, true);
                            }
                            else if (tries > 0)
                            {
                                _logger.LogError("Inputs were invalid, please try again...");
                            }
                            else
                            {
                                Directory.Delete(dirPath, true);
                                _logger.LogError("Maximum number of tries reached, aborting.");
                            }

                            _popUpClosed = true;
                        };
                        process.StartInfo.FileName = parsedCommand.FileName;
                        process.StartInfo.UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                        process.StartInfo.Arguments = $"{parsedCommand.Arguments} {path}";
                        process.Start();

                        int waitForMilliseconds = 100;
                        while (!_popUpClosed)
                        {
                            Thread.Sleep(waitForMilliseconds);
                        }
                    }
                }
            }
            catch (Win32Exception exc)
            {
                _logger.LogError(exc, $"Cannot start editor '{parsedCommand.FileName}'. Please verify that your git settings (`git config core.editor`) specify the path correctly.");
                result = Constants.ErrorCode;
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"There was an exception while trying to pop up an editor window.");
                result = Constants.ErrorCode;
            }

            return result;
        }

        public static ParsedCommand GetParsedCommand(string command)
        {
            ParsedCommand parsedCommand = new ParsedCommand();

            // If it's quoted then find the end of the quoted string.
            // If non quoted find a space or the end of the string.
            command = command.Trim();
            if (command.StartsWith("'") || command.StartsWith("\""))
            {
                int start = 1;
                int end = command.IndexOf("'", start);
                if (end == -1)
                {
                    end = command.IndexOf("\"", start);
                    if (end == -1)
                    {
                        // Unterminated quoted string.  Use full command as file name
                        parsedCommand.FileName = command.Substring(1);
                        return parsedCommand;
                    }
                }
                parsedCommand.FileName = command.Substring(start, end - start);
                parsedCommand.Arguments = command.Substring(end + 1);
                return parsedCommand;
            }
            else
            {
                // Find a space after the command name, if there are args, then parse them out,
                // otherwise just return the whole string as the filename.
                int fileNameEnd = command.IndexOf(" ");
                if (fileNameEnd != -1)
                {
                    parsedCommand.FileName = command.Substring(0, fileNameEnd);
                    parsedCommand.Arguments = command.Substring(fileNameEnd);
                }
                else
                {
                    parsedCommand.FileName = command;
                }
                return parsedCommand;
            }
        }
    }

    /// <summary>
    /// Process needs the file name and the arguments splitted apart. This represent these two.
    /// </summary>
    public class ParsedCommand
    {
        public string FileName { get; set; }

        public string Arguments { get; set; }
    }
}
