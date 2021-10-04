# 360 Stereoscopic Image Processing using Deep Learning#
In the field of Computer Vision we need a lot of data, so in order to acquire a large dataset for model training, we need to use synthetic data. Synthetic data has the advantaage that it can be generated in large amounts by using some programming without depending on manual labour. So, in order to do that we can use artificial worlds or scenarios created inside Unity and then capture that scene.

Therefore, inspired from the project [easy to add to any existing Unity project](https://bitbucket.org/Unity-Technologies/ml-imagesynthesis/src/master/), which allows to capture image depth, segmentation, optical flow, etc as .png images with minimal intrusion:

* __Image segmentation__ - each object in the scene gets unique color
* __Object categorization__ - objects are assigned color based on their category
* __Optical flow__ - pixels are colored according to their motion in the relation to camera
* __Depth__ - pixels are colored according to their distance from the camera
* __Normals__ - surfaces are colored according to their orientation in relation with the camera 

We modified the above code to capture 360 stereoscopic images and along with it the corresponding segemented image, object categorization, optical flow, depth and also normals. 

![alt text](https://github.com/subro608/Unity-Stereo/blob/main/helloImageSynthesis%2BCapturePass_1.jpg)
![alt text](https://github.com/subro608/Unity-Stereo/blob/main/helloImageSynthesis%2BCapturePass_2.jpg)
![alt text](https://github.com/subro608/Unity-Stereo/blob/main/helloImageSynthesis%2BCapturePass_3.jpg)
![alt text](https://github.com/subro608/Unity-Stereo/blob/main/helloImageSynthesis%2BCapturePass_4.jpg)
![alt text](https://github.com/subro608/Unity-Stereo/blob/main/helloImageSynthesis%2BCapturePass_5.jpg)




### Requirements ###
* Unity *5.5.0* or later
* Should work on any OS officially supported by Unity

### License ###
This repository is covered under the [MIT/X11](LICENSE.TXT) license.
