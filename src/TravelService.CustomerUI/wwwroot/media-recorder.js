let mediaRecorder;
let audioChunks = [];

window.startRecording = async function (dotNetHelper) {
   try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      mediaRecorder = new MediaRecorder(stream);

      mediaRecorder.ondataavailable = (event) => {
         if (event.data.size > 0) {
            let reader = new FileReader();
            reader.readAsArrayBuffer(event.data);
            reader.onloadend = function () {
               dotNetHelper.invokeMethodAsync('ProcessAudioChunk', new Uint8Array(reader.result));
            };
         }
      };

      mediaRecorder.start(1000);
   } catch (err) {
      console.error('Error accessing the microphone:', err);
   }
};

window.stopRecording = function () {
   if (mediaRecorder) {
      mediaRecorder.stop();
      mediaRecorder = null;
   }
};
