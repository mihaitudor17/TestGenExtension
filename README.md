# TestGenExtension
 AI Visual Studio extension that generates code for tests.
How to use:
Use the DataCollection project to generate a step_definitions.json file as training dataset. 
Note: You need to generate a GitHub API token and input instead of <YOUR_TOKEN> value for the Token in Program.cs
Use the Training.ipynb to generate a best_model folder and copy it to TestGen/model_server.
Use pyinstaller to generate an executable folder for TestGen/model-server/server.py where the best_model is included and run the extension.