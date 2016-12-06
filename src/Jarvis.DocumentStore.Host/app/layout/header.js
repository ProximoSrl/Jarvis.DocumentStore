(function (window, angular, undefined) {
    'use strict';

    angular
        .module('admin.layout')
        .directive('dsHeader', ['configService', dsHeader]);

    function dsHeader(configService) {
        var directive = {
            link: link,
            templateUrl: '/layout/header.html',
            restrict: 'E',
            replace:true
        };

        return directive;

        function link(scope, element, attrs) {
            configService.getVersion().then(function (res) {
                scope.info = res;
                console.log('INFO_HEADER', scope.info);
            });
            
        }
    };
})(window, window.angular);
