function ToggleRateMenu_Init() {
	for( var i = 0; i < RateMenus.length; i++ ) {
		var rateMenuInfo = RateMenus[ i ];
		ToggleRateMenu_Load( rateMenuInfo );
	}
}
function ToggleRateMenu_Load( rateMenuInfo ) {
	var rateButton = document.getElementById( rateMenuInfo.RateButtonID );
	rateButton.onclick = function() { ToggleRateMenu( rateMenuInfo.RateButtonID, rateMenuInfo.RateMenuID ); };
}

function ToggleRateMenu( rateButtonID, rateMenuID ) {
	rateMenu = document.getElementById( rateMenuID );
	rateButton = document.getElementById( rateButtonID );

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

function getposOffset(what, offsettype){
	var totaloffset=(offsettype=="left")? what.offsetLeft : what.offsetTop;
	var parentEl=what.offsetParent;
	while (parentEl!=null){
		totaloffset=(offsettype=="left")? totaloffset+parentEl.offsetLeft : totaloffset+parentEl.offsetTop;
		parentEl=parentEl.offsetParent;
	}
	return totaloffset;
}