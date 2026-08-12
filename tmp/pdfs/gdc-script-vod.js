// JavaScript source code
const queryString = window.location.search;

const urlParams = new URLSearchParams(queryString);

var videoId = urlParams.get('id')

// const room	= urlParams.get('room')

// const day = urlParams.get('day')

console.log(videoId);
// console.log(room);

function srcSwap() {
	PLAYBACK_URL = 'https://cdn-a.blazestreaming.com/out/v1/'+videoId+'/38549d9a185b445888c6fbc2e2e792e9/50413c5b8a95441e919f0a1579606e47/index.m3u8';
}

srcSwap();



