# Magneto 🧲

**Control software and system architecture for a custom Laser Powder Bed Fusion (LPBF) research platform.**

Magneto is a C#/.NET desktop application developed to coordinate the hardware of a custom metal additive-manufacturing system, including motion stages, laser control, vacuum and gas-handling equipment, and process monitoring.

The project was developed in the Tertuliano Lab at the University of Pennsylvania as part of an effort to build a flexible experimental LPBF platform for additive-manufacturing research.

## Demo

### Parameter Sweep Used to Test 316 Stainless Steel Print Quality
https://github.com/user-attachments/assets/7d73d123-3d96-40a3-ad41-83a312d9d1cc

### Software Print Page
<img width="1500" height="922" alt="2025-06-24_MAGNETO_EXAMPLE_test-print-page" src="https://github.com/user-attachments/assets/88771d2b-e7a7-49ce-bc56-0409844bad31" />

### App Tour Gif
![Magneto application demo](https://github.com/e-chesoni/Magneto/assets/57457528/e9cbd48a-1dcc-4c30-8323-057cb9715aee)

## Project Motivation

A custom research printer differs from a commercial machine in an important way: its hardware and experimental requirements continue to evolve.

Rather than tightly coupling the user interface to individual pieces of hardware, Magneto was designed around modular software components that separate:

* user interaction,
* machine state and application logic,
* hardware communication,
* device-specific implementations, and
* system-level coordination.

This allows individual devices and control strategies to be developed and tested independently while exposing a consistent interface to the rest of the application.

## System Architecture

The primary application architecture is divided between:

### `Magneto.Desktop.WinUI`

The WinUI application provides the operator-facing interface and application-level behavior.

The UI follows an MVVM-style organization, separating **Views** from **ViewModels** so that machine logic is not embedded directly in UI components.

Application functionality includes dedicated interfaces for:

* machine and process monitoring,
* print configuration and execution,
* motor testing,
* laser/Waverunner testing,
* maintenance and cleaning operations,
* material and process monitoring, and
* print history and settings.

### `Magneto.Desktop.WinUI.Core`

The Core project contains reusable machine and application logic independent of individual UI views.

It is organized around several software abstractions:

**Contracts** define interfaces between components.

**Services** encapsulate operations such as motor control, file handling, and data access.

**Factories** handle construction of hardware-specific objects while reducing coupling between application code and individual device implementations.

**Models** represent application and machine state.

Separating these components allows the UI to depend primarily on abstractions and services rather than directly controlling physical hardware.

## Hardware Abstraction

One of the central design goals of Magneto was to provide a common software architecture around heterogeneous hardware.

The system was developed to interface with components including:

* Micronix stepper and linear motion stages
* laser and scan-head hardware
* vacuum pumps and gauges
* oxygen monitoring
* mass-flow control
* inert-gas handling

Hardware-specific communication can therefore be isolated from higher-level application behavior.

For example, motor creation and motor-control behavior are separated through factory and service abstractions rather than requiring UI components to know how individual motor controllers communicate.

This structure was intended to make it easier to add, replace, or test hardware as the experimental platform evolved.

## Experimental Development

Because Magneto was developed alongside a custom physical machine, the repository also contains several proof-of-concept applications used during development.

Examples include:

* `LaserComsPOC`
* `MicronixCppPOC`
* `MotorQueuePOC`
* `MoveMotorsPOC`
* `SerialCommunicationPOC`

These projects were used to isolate communication and control problems before integrating functionality into the larger application.

Several desktop application architectures were also explored during development, including WPF and Windows App/WinUI approaches. The later architecture primarily centers on:

```text
Magneto.Desktop.WinUI
        │
        ├── Views / ViewModels
        │
        └── Application Services
                 │
                 ▼
Magneto.Desktop.WinUI.Core
        │
        ├── Contracts
        ├── Services
        ├── Factories
        └── Models
                 │
                 ▼
        Hardware Interfaces
                 │
        ┌────────┼─────────┐
        ▼        ▼         ▼
      Motors    Laser    Sensors /
                         Gas & Vacuum
```

This development process allowed hardware interfaces to be tested independently before being incorporated into the larger machine-control architecture.

## Design Patterns and Engineering Principles

Several design patterns and architectural principles are used throughout the project:

* **Model–View–ViewModel (MVVM)** to separate UI presentation from application behavior.
* **Service abstractions** to isolate machine operations from the UI.
* **Factory pattern** for constructing hardware-dependent objects.
* **Interface-based contracts** to reduce coupling between components.
* **Modular hardware abstraction** so device-specific communication can evolve without requiring corresponding changes throughout the application.
* **Proof-of-concept development** to validate communication with physical hardware before system integration.

The overall goal was to treat the printer not as a collection of independently controlled devices, but as a coordinated mechatronic system whose hardware could evolve while retaining a consistent software architecture.

## Repository Structure

```text
source/
├── Magneto.Desktop.WinUI/          # Primary WinUI application
├── Magneto.Desktop.WinUI.Core/     # Reusable application and machine logic
├── Magneto.Desktop.WinUI.Tests.MSTest/
│
├── LaserComsPOC/                   # Laser communication experiments
├── MicronixCppPOC/                 # Micronix communication experiments
├── MotorQueuePOC/                  # Motor command/queue experiments
├── MoveMotorsPOC/                  # Motion-control experiments
├── SerialCommunicationPOC/         # Serial communication experiments
│
├── Magneto.Desktop.WPF/            # Earlier desktop architecture
├── Magneto.Desktop.WindowsApp/     # Earlier desktop architecture
└── MagnetoLibrary/                 # Earlier shared-library implementation
```

The repository preserves these intermediate implementations because they document the iterative development process used to arrive at the later architecture.

## Technologies

* **C# / .NET**
* **WinUI**
* **XAML**
* **MVVM**
* Serial/device communication
* Hardware abstraction and service-oriented application architecture

## Status

Magneto was developed as a research tool for a custom Laser Powder Bed Fusion (LPBF) system. The machine was completed in **June 2025** and successfully used to produce metal test specimens.

The repository represents both the resulting control architecture and the engineering process used to prototype, test, and integrate the subsystems of a custom mechatronic research platform.

