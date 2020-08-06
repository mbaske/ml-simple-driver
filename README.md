## Simple Driver

This is a basic car controller demo for [Unity Machine Learning Agents](https://github.com/Unity-Technologies/ml-agents) [v0.12](https://github.com/Unity-Technologies/ml-agents/releases/tag/0.12.0).  

The driver agent can control acceleration and steering, while observing the road either with raycasts or cameras. The raycast detection method is fitted to the specific road type (using spline points) and probably won't generalize well to other situations. The project contains pretrained models for both setups which are trained up to a point where they can manage the easier track layouts, but still fail on the more curved ones. Road generation and raycast detection code are stripped down versions of what was used for [this project](https://www.youtube.com/watch?v=gEf9V03HWv0).

Dependencies:  
https://github.com/Unity-Technologies/ml-agents/releases/tag/0.12.0
