(function(window, angular, undefined) {'use strict';

    angular.module('admin', [
        'ui.router',
        'admin.shared',
        'admin.layout',
        'admin.dashboard',
        'admin.info'
    ]);

})(window, window.angular);
