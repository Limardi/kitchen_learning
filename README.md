# step by step how to implement the code:

- download unity editor (2022 version)
- download unity MLAgents, you can follow https://www.immersivelimit.com/tutorials/reinforcement-learning-penguins-part-4-unity-ml-agents
- download the game code in https://cmonkey.co/freecourse (search for kitchen chaos)
- set all the file inside the Asset folder by the code in this repo
- download anaconda, you can follow this link (https://www.immersivelimit.com/tutorials/ml-agents-python-setup-anaconda)
- activate the virtual environment
- run this line "mlagents-learn {your directory}/chef.yaml --run-id {you can fill the name}"
- it will show wher it save the .onnx file after training.
- put the .onnx file in the player tree in the unity editor.
- run the game, it will run the player with the model you just trained.
- if you got any trouble integrating unity ml agents, please refer to https://www.immersivelimit.com/tutorials/reinforcement-learning-penguins-part-4-unity-ml-agents

please kindly contact us if you need some help.
  email: vincentlimardi234@gmail.com
