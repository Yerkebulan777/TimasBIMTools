using Autodesk.Revit.DB;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;


namespace RevitTimasBIMTools.RevitUtils
{
    internal class ProjectLoctionManager
    {


        public void ShowActiveProjectLocationUsage(Autodesk.Revit.DB.Document document)
        {
            // Get the project location handle 
            ProjectLocation projectLocation = document.ActiveProjectLocation;

            // Show the information of current project location
            XYZ origin = new XYZ(0, 0, 0);
            ProjectPosition position = projectLocation.GetProjectPosition(origin);
            if (null == position)
            {
                throw new Exception("No project position in origin point.");
            }

            // Format the prompt string to show the message.
            String prompt = "Current project location information:\n";
            prompt += "\n\t" + "Origin point position:";
            prompt += "\n\t\t" + "Angle: " + position.Angle;
            prompt += "\n\t\t" + "East to West offset: " + position.EastWest;
            prompt += "\n\t\t" + "Elevation: " + position.Elevation;
            prompt += "\n\t\t" + "North to South offset: " + position.NorthSouth;

            // Angles are in radians when coming from Revit API, so we 
            // convert to degrees for display
            const double angleRatio = Math.PI / 180;        // angle conversion factor

            SiteLocation site = projectLocation.GetSiteLocation();
            string degreeSymbol = ((char)176).ToString();
            prompt += "\n\t" + "Site location:";
            prompt += "\n\t\t" + "Latitude: " + site.Latitude / angleRatio + degreeSymbol;
            prompt += "\n\t\t" + "Longitude: " + site.Longitude / angleRatio + degreeSymbol;
            prompt += "\n\t\t" + "TimeZone: " + site.TimeZone;

            LogManager.Info(prompt);
        }


        public ProjectLocation DuplicateLocation(Autodesk.Revit.DB.Document document, string newName)
        {
            ProjectLocation currentLocation = document.ActiveProjectLocation;
            ProjectLocationSet locations = document.ProjectLocations;
            foreach (ProjectLocation projectLocation in locations)
            {
                if (projectLocation.Name == newName)
                {
                    throw new Exception("The name is same as a project location's name, please change one.");
                }
            }
            return currentLocation.Duplicate(newName);
        }


        public void DeleteLocation(Autodesk.Revit.DB.Document document)
        {
            ProjectLocation currentLocation = document.ActiveProjectLocation;
            //There must be at least one project location in the project.
            ProjectLocationSet locations = document.ProjectLocations;
            if (1 == locations.Size)
            {
                return;
            }

            string name = "location";
            if (name != currentLocation.Name)
            {
                foreach (ProjectLocation projectLocation in locations)
                {
                    if (projectLocation.Name == name)
                    {
                        ICollection<Autodesk.Revit.DB.ElementId> elemSet = document.Delete(projectLocation.Id);
                        if (elemSet.Count > 0)
                        {
                            LogManager.Info("Project Location Deleted!");
                        }
                    }
                }
            }
        }

    }
}
