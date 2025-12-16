class WebRTCCallManager {
    constructor(signalRConnection) {
        this.signalRConnection = signalRConnection;
        this.localStream = null;
        this.peerConnections = new Map();
        this.currentCallId = null;
        this.isAudioEnabled = true;
        this.isVideoEnabled = false;
        this.isScreenSharing = false;
        this.videoQuality = 'HD'; // HD, SD, LD
        this.configuration = {
            iceServers: [
                { urls: 'stun:stun.l.google.com:19302' },
                { urls: 'stun:stun1.l.google.com:19302' }
            ]
        };
    }

    getVideoConstraints(quality = this.videoQuality) {
        const constraints = {
            audio: true,
            video: false
        };

        if (quality === 'HD') {
            constraints.video = {
                width: { ideal: 1280 },
                height: { ideal: 720 },
                frameRate: { ideal: 30 }
            };
        } else if (quality === 'SD') {
            constraints.video = {
                width: { ideal: 640 },
                height: { ideal: 480 },
                frameRate: { ideal: 24 }
            };
        } else if (quality === 'LD') {
            constraints.video = {
                width: { ideal: 320 },
                height: { ideal: 240 },
                frameRate: { ideal: 15 }
            };
        }

        return constraints;
    }

    async startCall(chatId, callType) {
        try {
            // Get user media with appropriate quality
            const constraints = { audio: true, video: false };
            if (callType === 'Video') {
                constraints.video = this.getVideoConstraints().video;
            }

            this.localStream = await navigator.mediaDevices.getUserMedia(constraints);
            this.isVideoEnabled = callType === 'Video';

            // Start the call via SignalR
            await this.signalRConnection.invoke('StartCall', chatId, callType);
        } catch (error) {
            console.error('Error starting call:', error);
            if (error.name === 'NotAllowedError') {
                throw new Error('Microphone/camera access denied. Please allow access to make calls.');
            } else if (error.name === 'NotFoundError') {
                throw new Error('No microphone/camera found. Please check your devices.');
            } else if (error.name === 'NotReadableError') {
                throw new Error('Microphone/camera is already in use by another application.');
            } else {
                throw new Error(`Failed to access media devices: ${error.message}`);
            }
        }
    }

    async joinCall(callId) {
        try {
            this.currentCallId = callId;

            // Get user media if not already obtained
            if (!this.localStream) {
                const constraints = { audio: true, video: this.isVideoEnabled };
                this.localStream = await navigator.mediaDevices.getUserMedia(constraints);
            }

            // Join the call via SignalR
            await this.signalRConnection.invoke('JoinCall', callId);

            // Set up SignalR event handlers for WebRTC signaling
            this.setupSignalingHandlers();
        } catch (error) {
            console.error('Error joining call:', error);
            if (error.name === 'NotAllowedError') {
                throw new Error('Microphone/camera access denied. Please allow access to join the call.');
            } else if (error.name === 'NotFoundError') {
                throw new Error('No microphone/camera found. Please check your devices.');
            } else if (error.name === 'NotReadableError') {
                throw new Error('Microphone/camera is already in use by another application.');
            } else {
                throw new Error(`Failed to access media devices: ${error.message}`);
            }
        }
    }

    setupSignalingHandlers() {
        this.signalRConnection.on('ReceiveOffer', async (data) => {
            await this.handleOffer(data);
        });

        this.signalRConnection.on('ReceiveAnswer', async (data) => {
            await this.handleAnswer(data);
        });

        this.signalRConnection.on('ReceiveIceCandidate', async (data) => {
            await this.handleIceCandidate(data);
        });

        this.signalRConnection.on('CallParticipantJoined', (data) => {
            this.handleParticipantJoined(data);
        });

        this.signalRConnection.on('CallParticipantLeft', (data) => {
            this.handleParticipantLeft(data);
        });
    }

    async handleOffer(data) {
        const { callId, fromUserId, offer } = data;

        try {
            const peerConnection = this.createPeerConnection(fromUserId);
            await peerConnection.setRemoteDescription(new RTCSessionDescription(JSON.parse(offer)));

            const answer = await peerConnection.createAnswer();
            await peerConnection.setLocalDescription(answer);

            await this.signalRConnection.invoke('SendAnswer', callId, fromUserId, JSON.stringify(answer));
        } catch (error) {
            console.error('Error handling offer:', error);
        }
    }

    async handleAnswer(data) {
        const { fromUserId, answer } = data;

        try {
            const peerConnection = this.peerConnections.get(fromUserId);
            if (peerConnection) {
                await peerConnection.setRemoteDescription(new RTCSessionDescription(JSON.parse(answer)));
            }
        } catch (error) {
            console.error('Error handling answer:', error);
        }
    }

    async handleIceCandidate(data) {
        const { fromUserId, candidate } = data;

        try {
            const peerConnection = this.peerConnections.get(fromUserId);
            if (peerConnection) {
                await peerConnection.addIceCandidate(new RTCIceCandidate(JSON.parse(candidate)));
            }
        } catch (error) {
            console.error('Error handling ICE candidate:', error);
        }
    }

    handleParticipantJoined(data) {
        const { callId, userId } = data;

        // Create peer connection for new participant
        if (userId !== this.getCurrentUserId()) {
            this.createPeerConnection(userId);
            this.createOffer(callId, userId);
        }
    }

    handleParticipantLeft(data) {
        const { userId } = data;

        // Close and remove peer connection
        const peerConnection = this.peerConnections.get(userId);
        if (peerConnection) {
            peerConnection.close();
            this.peerConnections.delete(userId);
        }

        // Remove video element
        this.removeParticipantVideo(userId);
    }

    createPeerConnection(userId) {
        const peerConnection = new RTCPeerConnection(this.configuration);

        // Add local stream tracks
        if (this.localStream) {
            this.localStream.getTracks().forEach(track => {
                peerConnection.addTrack(track, this.localStream);
            });
        }

        // Handle ICE candidates
        peerConnection.onicecandidate = (event) => {
            if (event.candidate && this.currentCallId) {
                this.signalRConnection.invoke('SendIceCandidate', this.currentCallId, userId, JSON.stringify(event.candidate));
            }
        };

        // Handle remote stream
        peerConnection.ontrack = (event) => {
            this.addParticipantVideo(userId, event.streams[0]);
        };

        // Handle connection state changes
        peerConnection.onconnectionstatechange = () => {
            console.log(`Connection state for ${userId}:`, peerConnection.connectionState);
        };

        this.peerConnections.set(userId, peerConnection);
        return peerConnection;
    }

    async createOffer(callId, targetUserId) {
        try {
            const peerConnection = this.peerConnections.get(targetUserId);
            if (!peerConnection) return;

            const offer = await peerConnection.createOffer();
            await peerConnection.setLocalDescription(offer);

            await this.signalRConnection.invoke('SendOffer', callId, targetUserId, JSON.stringify(offer));
        } catch (error) {
            console.error('Error creating offer:', error);
        }
    }

    addParticipantVideo(userId, stream) {
        // Create or update video element for participant
        let videoElement = document.getElementById(`participant-${userId}`);
        if (!videoElement) {
            videoElement = document.createElement('video');
            videoElement.id = `participant-${userId}`;
            videoElement.autoplay = true;
            videoElement.muted = true; // Avoid feedback
            videoElement.style.width = '200px';
            videoElement.style.height = '150px';
            videoElement.style.border = '1px solid #ccc';
            videoElement.style.margin = '5px';

            const container = document.getElementById('call-participants');
            if (container) {
                container.appendChild(videoElement);
            }
        }

        videoElement.srcObject = stream;
    }

    removeParticipantVideo(userId) {
        const videoElement = document.getElementById(`participant-${userId}`);
        if (videoElement) {
            videoElement.remove();
        }
    }

    async toggleMute() {
        if (!this.localStream) return;

        const audioTracks = this.localStream.getAudioTracks();
        audioTracks.forEach(track => {
            track.enabled = !track.enabled;
        });

        this.isAudioEnabled = !this.isAudioEnabled;

        if (this.currentCallId) {
            await this.signalRConnection.invoke('ToggleMute', this.currentCallId);
        }

        return this.isAudioEnabled;
    }

    async toggleVideo() {
        if (!this.localStream) return;

        if (this.isVideoEnabled) {
            // Turn off video
            const videoTracks = this.localStream.getVideoTracks();
            videoTracks.forEach(track => {
                track.stop();
                this.localStream.removeTrack(track);
            });
            this.isVideoEnabled = false;
        } else {
            // Turn on video with current quality settings
            try {
                const constraints = this.getVideoConstraints();
                const newStream = await navigator.mediaDevices.getUserMedia({ video: constraints.video });
                const videoTrack = newStream.getVideoTracks()[0];
                this.localStream.addTrack(videoTrack);
                this.isVideoEnabled = true;

                // Update all peer connections
                this.peerConnections.forEach(peerConnection => {
                    const sender = peerConnection.getSenders().find(s => s.track?.kind === 'video');
                    if (sender) {
                        sender.replaceTrack(videoTrack);
                    } else {
                        peerConnection.addTrack(videoTrack, this.localStream);
                    }
                });
            } catch (error) {
                console.error('Error enabling video:', error);
                return false;
            }
        }

        if (this.currentCallId) {
            await this.signalRConnection.invoke('ToggleVideo', this.currentCallId);
        }

        return this.isVideoEnabled;
    }

    async changeVideoQuality(quality) {
        this.videoQuality = quality;

        if (this.isVideoEnabled && this.localStream) {
            try {
                // Get new video constraints
                const constraints = this.getVideoConstraints();

                // Replace the current video track with new quality
                const videoTracks = this.localStream.getVideoTracks();
                if (videoTracks.length > 0) {
                    const newStream = await navigator.mediaDevices.getUserMedia({ video: constraints.video });
                    const newVideoTrack = newStream.getVideoTracks()[0];

                    // Replace track in all peer connections
                    this.peerConnections.forEach(peerConnection => {
                        const sender = peerConnection.getSenders().find(s => s.track?.kind === 'video');
                        if (sender) {
                            sender.replaceTrack(newVideoTrack);
                        }
                    });

                    // Replace track in local stream
                    const oldTrack = videoTracks[0];
                    this.localStream.removeTrack(oldTrack);
                    this.localStream.addTrack(newVideoTrack);
                    oldTrack.stop();
                }
            } catch (error) {
                console.error('Error changing video quality:', error);
                throw error;
            }
        }
    }

    async toggleScreenShare() {
        if (this.isScreenSharing) {
            // Stop screen sharing
            if (this.localStream) {
                const videoTracks = this.localStream.getVideoTracks();
                videoTracks.forEach(track => {
                    if (track.getSettings().displaySurface) {
                        track.stop();
                        this.localStream.removeTrack(track);
                    }
                });
            }
            this.isScreenSharing = false;
        } else {
            // Start screen sharing
            try {
                const screenStream = await navigator.mediaDevices.getDisplayMedia({ video: true });
                const screenTrack = screenStream.getVideoTracks()[0];

                // Replace video track in all peer connections
                this.peerConnections.forEach(peerConnection => {
                    const sender = peerConnection.getSenders().find(s => s.track?.kind === 'video');
                    if (sender) {
                        sender.replaceTrack(screenTrack);
                    }
                });

                // Handle when user stops sharing via browser UI
                screenTrack.onended = () => {
                    this.toggleScreenShare();
                };

                this.isScreenSharing = true;
            } catch (error) {
                console.error('Error starting screen share:', error);
                return false;
            }
        }

        if (this.currentCallId) {
            await this.signalRConnection.invoke('ToggleScreenShare', this.currentCallId);
        }

        return this.isScreenSharing;
    }

    async leaveCall() {
        if (this.currentCallId) {
            await this.signalRConnection.invoke('LeaveCall', this.currentCallId);
        }

        this.cleanup();
    }

    async endCall() {
        if (this.currentCallId) {
            await this.signalRConnection.invoke('EndCall', this.currentCallId);
        }

        this.cleanup();
    }

    cleanup() {
        // Close all peer connections
        this.peerConnections.forEach(peerConnection => {
            peerConnection.close();
        });
        this.peerConnections.clear();

        // Stop local stream
        if (this.localStream) {
            this.localStream.getTracks().forEach(track => track.stop());
            this.localStream = null;
        }

        // Remove all participant videos
        const container = document.getElementById('call-participants');
        if (container) {
            container.innerHTML = '';
        }

        this.currentCallId = null;
        this.isAudioEnabled = true;
        this.isVideoEnabled = false;
        this.isScreenSharing = false;
    }

    getCurrentUserId() {
        // This should be implemented to get the current user ID from authentication
        // For now, return a default value - this should be replaced with proper auth integration
        return 'user1';
    }
}

// Global instance
let webRTCCallManager = null;

// JavaScript interop functions
window.initializeWebRTCCallManager = function(signalRConnection) {
    webRTCCallManager = new WebRTCCallManager(signalRConnection);
};

window.startWebRTCCall = async function(chatId, callType) {
    if (webRTCCallManager) {
        await webRTCCallManager.startCall(chatId, callType);
    }
};

window.joinWebRTCCall = async function(callId) {
    if (webRTCCallManager) {
        await webRTCCallManager.joinCall(callId);
    }
};

window.toggleWebRTCMute = async function() {
    if (webRTCCallManager) {
        return await webRTCCallManager.toggleMute();
    }
    return false;
};

window.toggleWebRTCVideo = async function() {
    if (webRTCCallManager) {
        return await webRTCCallManager.toggleVideo();
    }
    return false;
};

window.toggleWebRTCScreenShare = async function() {
    if (webRTCCallManager) {
        return await webRTCCallManager.toggleScreenShare();
    }
    return false;
};

window.endWebRTCCall = async function() {
    if (webRTCCallManager) {
        await webRTCCallManager.endCall();
    }
};

window.leaveWebRTCCall = async function() {
    if (webRTCCallManager) {
        await webRTCCallManager.leaveCall();
    }
};

window.changeVideoQuality = async function(quality) {
    if (webRTCCallManager) {
        await webRTCCallManager.changeVideoQuality(quality);
    }
};

window.isWebRTCSupported = function() {
    return !!(window.RTCPeerConnection || window.webkitRTCPeerConnection || window.mozRTCPeerConnection) &&
           !!navigator.mediaDevices &&
           !!navigator.mediaDevices.getUserMedia;
};

// Export for use in other scripts
window.WebRTCCallManager = WebRTCCallManager;