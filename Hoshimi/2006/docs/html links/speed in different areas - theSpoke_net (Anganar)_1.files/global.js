
	function findObj(theObj, theDoc)
	{
		var p, i, foundObj;
		
		if(!theDoc) theDoc = document;
		if( (p = theObj.indexOf("?")) > 0 && parent.frames.length)
		{
			theDoc = parent.frames[theObj.substring(p+1)].document;
			theObj = theObj.substring(0,p);
		}
		if(!(foundObj = theDoc[theObj]) && theDoc.all) foundObj = theDoc.all[theObj];
		for (i=0; !foundObj && i < theDoc.forms.length; i++) 
			foundObj = theDoc.forms[i][theObj];
		for(i=0; !foundObj && theDoc.layers && i < theDoc.layers.length; i++) 
			foundObj = findObj(theObj,theDoc.layers[i].document);
		if(!foundObj && document.getElementById) foundObj = document.getElementById(theObj);
		
		return foundObj;
	}
	
	function showHideLayers()
	{ 
		var i, visStr, obj, args = showHideLayers.arguments;
		for (i=0; i<(args.length-2); i+=3)
		{
			if ((obj = findObj(args[i])) != null)
			{
				visStr = args[i+2];
				if (obj.style)
				{
					obj = obj.style;
					if(visStr == 'show') visStr = 'visible';
					else if(visStr == 'hide') visStr = 'hidden';
				}
				obj.visibility = visStr;
			}
		}
	}
	
	function preloadImages()
	{
		if(document.images)
		{
			if(!document.imageArray) document.imageArray = new Array();
			var i,j = document.imageArray.length, args = preloadImages.arguments;
			
			for(i=0; i<args.length; i++)
			{
				if (args[i].indexOf("#")!=0)
				{
					document.imageArray[j] = new Image;
					document.imageArray[j++].src = args[i];
				}
			}
		}
	}
	


function ShowHide(id) { 
	obj = document.getElementsByTagName("div"); 
	if (obj[id].style.visibility == 'visible'){
		obj[id].style.display = 'block'; 
		obj[id].style.visibility = 'hidden'; 
	} else {
		obj[id].style.visibility = 'visible'; 
		obj[id].style.display = 'block';
	}
}

function Show(id) { 
    obj = document.getElementsByTagName("div");  
    obj[id].style.visibility = 'visible'; 
} 
function Hide(id) { 
        obj = document.getElementsByTagName("div");
        obj[id].style.visibility = 'hidden'; 
} 

function ShowDisplay(id) { 
    obj = document.getElementsByTagName("div"); 
    obj[id].style.visibility = 'visible';  
    obj[id].style.display = 'block'; 
} 
function HideDisplay(id) { 
        obj = document.getElementsByTagName("div");
        obj[id].style.visibility = 'hidden'; 
        obj[id].style.display = 'none'; 
} 

function ToggleSendToFriend() {
    sendToFriendForm = document.getElementById('SendToFriendForm');
    sendToFriendButton = document.getElementById('SendToFriendButton');

    if (sendToFriendForm.style.visibility == "hidden") {
      sendToFriendForm.style.visibility = "visible";
      sendToFriendForm.style.display = 'block';
    } else {
      sendToFriendForm.style.visibility = "hidden";
      sendToFriendForm.style.display = 'none';
    }

    //sendToFriendForm.style.left = getposOffset(sendToFriendButton, "left") - sendToFriendForm.offsetWidth + sendToFriendButton.offsetWidth;
    sendToFriendForm.style.left = getposOffset(sendToFriendButton, "left") - (sendToFriendForm.offsetWidth / 2) + (sendToFriendButton.offsetWidth / 2);
    sendToFriendForm.style.top = getposOffset(sendToFriendButton, "top") + sendToFriendButton.offsetHeight;
  }
  
  function ToggleAddComment() {
    commentMenu = document.getElementById('CommentMenu');
    commentButton = document.getElementById('CommentButton');

    commentMenu.style.left = getposOffset(commentButton, "left");
    commentMenu.style.top = getposOffset(commentButton, "top") + commentButton.offsetHeight;

    if (commentMenu.style.visibility == "hidden") {
      commentMenu.style.visibility = "visible";
      commentMenu.style.display = 'block';
    } else {
      commentMenu.style.visibility = "hidden";
      commentMenu.style.display = 'none';
    }
  }
  
  function ToggleGalleryThumbnail(pictureID) {
    largeThumbDiv = document.getElementById('SecondaryThumbDiv' + pictureID);
    smallThumb = document.getElementById('SmallThumb' + pictureID);

    if (largeThumbDiv.className == "secondaryThumbnailHidden") {
      largeThumbDiv.className = "secondaryThumbnailPopup";
      largeThumbDiv.style.left = getposOffset(smallThumb, "left") - ((largeThumbDiv.offsetWidth - smallThumb.offsetWidth) / 2) + "px";
      largeThumbDiv.style.top = getposOffset(smallThumb, "top")  - ((largeThumbDiv.offsetHeight - smallThumb.offsetHeight) / 2) + "px";
      setTimeout(function() { largeThumbDiv.style.visibility = "visible"; }, 5);
    } else {
	  largeThumbDiv.className = "secondaryThumbnailHidden";
    }
  }
  
  function ToggleRateMenu() {
    rateMenu = document.getElementById('RateMenu');
    rateButton = document.getElementById('RateButton');

    rateMenu.style.left = getposOffset(rateButton, "left");
    rateMenu.style.top = getposOffset(rateButton, "top") + rateButton.offsetHeight;

    if (rateMenu.style.visibility == "hidden") {
      rateMenu.style.visibility = "visible";
      rateMenu.style.display = 'block';
    } else {
      rateMenu.style.visibility = "hidden";
      rateMenu.style.display = 'none';
    }
  }

  function ToggleSearchMenu() {
    searchMenu = document.getElementById('SearchMenu');
    searchButton = document.getElementById('SearchButton');

    searchMenu.style.left = getposOffset(searchButton, "left");
    searchMenu.style.top = getposOffset(searchButton, "top") + searchButton.offsetHeight;

    if (searchMenu.style.visibility == "hidden") {
      searchMenu.style.visibility = "visible";
      searchMenu.style.display = 'block';
    } else {
      searchMenu.style.visibility = "hidden";
      searchMenu.style.display = 'none';
    }
  }

function getposOffset(what, offsettype){
  var totaloffset=(offsettype=="left")? what.offsetLeft : what.offsetTop;
  var parentEl=what.offsetParent;
  while (parentEl!=null){
    totaloffset=(offsettype=="left")? totaloffset+parentEl.offsetLeft : totaloffset+parentEl.offsetTop;
    parentEl=parentEl.offsetParent;
  }
  return totaloffset;
}

function ToggleMenuOnOff (menuName) {
    menu = document.getElementById(menuName);

    if (menu.style.display == 'none') {
      menu.style.display = 'block';
    } else {
      menu.style.display = 'none';
    }

}

function OpenWindow (target) { 
  window.open(target, "_Child", "toolbar=no,scrollbars=yes,resizable=yes,width=400,height=400"); 
}

function OpenPostWindow (target) { 
  window.open(target, "_Child", "resizable=yes,width=500,height=700"); 
}