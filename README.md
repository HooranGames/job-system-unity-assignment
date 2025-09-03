# Unity Assignment – Robot Task System

The game takes place on a **distant planet**, home to a few **old robots** and some **lazy humans**.  

---

## Robots
There are three robots, each with a distinct role:

- **Chobin** – Scientist  
- **Delores** – Miner  
- **Ern** – (to be defined)  

---

## Task / Job System
### Available Task Types
- Research  
- Mining  
- Cooking  

### Task Creation
- The player can create new tasks at any time.  
- To create a task:
  1. Select one of the task types.  
  2. Click on the scene to specify its location.  
- A **UI button** exists for creating new tasks.  

### Task Execution
- Robots automatically seek out and complete tasks.  
- Each task takes a certain amount of time (e.g., 30 seconds).  
- Task duration varies depending on the task type.  
- While a task is in progress:  
  - A **loading bar** is shown above the task location.  
  - The loading bar displays **progress** and the **task name**.  
- The system must allow **easy addition of new task types**.  

### Idle Behavior
- If no tasks exist:  
  - Robots enter a **sleep state**.  
  - A floating text (e.g., *“Sleeping...”*) appears above their heads.  
  - Their animation switches to **idle**.  

---

## Movement & Animation
- Robots move to the task location (**no movement animation required**).  
- Idle animation:  
  - A simple **hovering effect** (floating up and down).  

---

## Assets
- Robot sprites are already included in the project.  
