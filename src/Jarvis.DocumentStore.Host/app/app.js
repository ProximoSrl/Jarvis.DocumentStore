(function(window, angular, undefined) {'use strict';

    angular.module('admin', [
        'ui.router',
        'admin.shared',
        'admin.layout',
        'admin.dashboard',
        'admin.info',
        'admin.logs',
        'angularUtils.directives.uiBreadcrumbs',
        'angularUtils.directives.dirPagination'
    ]);

})(window, window.angular);
