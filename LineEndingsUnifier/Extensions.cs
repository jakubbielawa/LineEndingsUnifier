using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;

namespace JakubBielawa.LineEndingsUnifier
{
    public static class Extensions
    {
        public static bool EndsWithAny(this string str, string[] strings)
        {
            foreach (var s in strings)
            {
                if (str.EndsWith(s))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool EqualsAny(this string str, string[] strings)
        {
            foreach (var s in strings)
            {
                if (str.Equals(s))
                {
                    return true;
                }
            }
            return false;
        }

        public static IList<Project> GetAllProjects(this Solution solution)
        {
            Projects projects = solution.Projects;
            List<Project> list = new List<Project>();
            var item = projects.GetEnumerator();

            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    list.Add(project);
                }
            }

            return list;
        }

        private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            List<Project> list = new List<Project>();

            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    list.Add(subProject);
                }
            }

            return list;
        }
    }
}
