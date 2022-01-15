var lineLayer = new mapvgl.LineRainbowLayer({
	style: 'road', // road, arrow, normal
	width: 15,
	color: [{ $COLORS }]
});
view.addLayer(lineLayer);

var data = [{
	geometry: {
		type: 'LineString',
		coordinates: [
			{ $CORDS }
		]
	}
}];
lineLayer.setData(data);