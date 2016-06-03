var gulp = require('gulp');
var nuget = require('gulp-nuget');
var sprintf = require("sprintf-js").sprintf;
var shell = require('gulp-shell')
var mkdirp = require('mkdirp');

var settings = {
	version : '0.0.11',
	packageName : 'RingCentral',
	nuget : {
		path : 'nuget',
		spec : function(s) {return sprintf ('%s.nuspec', s.packageName);},
		nupkgFile : function(s) {return sprintf('%s/%s.%s.nupkg', s.nuget.nupkgFolder(s), s.packageName, s.version);},
		nupkgFolder : function(s) {return sprintf('packages');},
		source : 'https://artifactory.intelliflo.com/artifactory/api/nuget/nuget-local',
		// 'c:/work/nuget_feed/',
		auth : {
			ApiKey : 'stuart',
			
		}
	}
};

console.log('version: ' + settings.version)
console.log('nuspec file: ' + settings.nuget.spec(settings))
console.log('nupkg output into fodler: ' + settings.nuget.nupkgFolder(settings))


// Creating packages folder,
mkdirp(settings.nuget.nupkgFolder(settings, function (err) {
    if (err) console.error(err)
    else console.log('pow!');
}));

gulp.task('nuget-pack', shell.task([
	sprintf('nuget pack %s -OutputDirectory %s -Version %s', settings.nuget.spec(settings), settings.nuget.nupkgFolder(settings), settings.version )
]));

gulp.task('nuget-push', shell.task([
	sprintf('nuget push %s %s -s %s', settings.nuget.nupkgFile(settings), settings.nuget.auth.ApiKey, settings.nuget.source )
]));


gulp.task('default', function(){
	console.log('How to publish new versions of Gulp.Tools')
	console.log('1. Bump version in gulpfile.js, settings.version property.')
	console.log('2. Run "gulp nuget-pack" to create a nuget package file.')
	console.log('3. Run "gulp nuget-push" to upload this file to intelliflo nuget server.')
	console.log('4. update microservice to use newer version of gulp tools')
});

