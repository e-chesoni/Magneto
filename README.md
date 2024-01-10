<h1 align="center">Welcome to Magneto 🧲 </h1>

## TLDR;
Custom Metal 3D Printer Control Software

<h2 align="left">Demo:</h2>

![App_Tour](https://github.com/e-chesoni/Magneto/assets/57457528/e9cbd48a-1dcc-4c30-8323-057cb9715aee)

🔭 This project is currently in progress: [Magneto](https://github.com/e-chesoni/Magneto)

## Project Overview
This project, developed as part of Tertuliano Lab at the University of Pennsylvania, is dedicated to creating control software for a custom metal 3D printer. Our goal is to explore and expand the possibilities in additive manufacturing, focusing on applications that can benefit biological and space manufacturing.

### What the Magneto Does
The software controls motors, a laser, and a gas pump, and monitors various parameters to facilitate additive manufacturing in 3D metal printing.

### Technologies Used
- Micronix Motors (Stepper and Linear)
- InstruTech Vacuum Guage Controller
- Busy Bee Vacuum Guage
- Thor Labs Scan Head
- Thor Labs Laser
- Hicube 80 Eco Station Gas Pump

### Next Steps
#### Testing the Laser Integration
Development has been completed on code that interfaces with the Waverunner software, responsible for controlling the laser. The immediate next step involves testing this software to ensure its effectiveness and reliability in laser control.

#### Integration and Monitoring of External Components
Focus will then shift to testing read and write operations with the remaining external components, which include the oxygen monitor, mass flow controller, and vacuum gauge controller and monitor.
Successful testing will be followed by the integration of code to enable real-time reading and control of these components, ensuring precision and safety in the 3D metal printing process.

## Prerequisites
- Visual Studio Code or any preferred C# IDE
- .NET Core SDK
- Git

## Getting Started
These instructions will guide you on how to obtain and run a copy of this project on your local machine for development and testing purposes.

### Fork the Repository
1. Visit the GitHub repository URL.
2. Click on the 'Fork' button at the top right corner to create a fork of the repository.

### Clone the Repository
1. Open your terminal (Command Prompt, PowerShell, or any preferred terminal).
2. Navigate to the directory where you want to clone the project.
3. Run the following command:
   ```
   git clone https://github.com/e-chesoni/Magneto
   ```
4. Navigate into the cloned directory.

### Open the Project in Your IDE
1. Open Visual Studio Code or your preferred IDE.
2. Go to File > Open Folder (or the equivalent in your IDE).
3. Select the cloned project directory and open it.

### Install Dependencies
1. Open the terminal in your IDE.
2. Run the following command to install the required dependencies:
   ```
   dotnet restore
   ```

### Running the Project
1. In the terminal, navigate to the project directory.
2. Run the project using:
   ```
   dotnet run
   ```

## Contributing
We welcome contributions from the community. Here’s how you can contribute:

1. Fork the project.
2. Create your feature branch (`git checkout -b feature/YourFeature`).
3. Commit your changes (`git commit -m 'Add some YourFeature'`).
4. Push to the branch (`git push origin feature/YourFeature`).
5. Open a pull request.

## License
This project is licensed under MIT

<h3 align="left">Languages and Tools:</h3>
<p align="left"> <a href="https://www.w3schools.com/cs/" target="_blank" rel="noreferrer"> <img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/csharp/csharp-original.svg" alt="csharp" width="40" height="40"/> </a> </p>
