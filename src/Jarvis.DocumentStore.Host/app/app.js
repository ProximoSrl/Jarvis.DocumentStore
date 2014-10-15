(function(window, angular, undefined) {'use strict';

    angular.module('admin', [
        'ui.router',
        'admin.shared',
        'admin.layout',
        'admin.dashboard',
        'admin.info',
        'angularUtils.directives.uiBreadcrumbs'
    ]);

})(window, window.angular);
